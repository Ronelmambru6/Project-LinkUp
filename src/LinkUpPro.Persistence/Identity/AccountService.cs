using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Persistence.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Persistence.Identity
{
    // Vive en Persistence porque usa UserManager<ApplicationUser>, que depende de Identity.
    // La interfaz está en Application, así Web no conoce los detalles de Identity.
    public class AccountService : IAccountService
    {
        private const string CarpetaFotos = "profiles";

        // Huella del último enlace de restablecimiento, guardada en la tabla AspNetUserTokens
        private const string ProveedorTokenPropio = "LinkUpPro";
        private const string NombreTokenRestablecimiento = "UltimoTokenRestablecimiento";
        private static readonly TimeSpan EsperaReenvio = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DuracionSesionPersistente = TimeSpan.FromDays(7);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IImageStorageService _imageStorage;
        private readonly IEmailService _emailService;
        private readonly EmailConfirmationTokenProvider<ApplicationUser> _confirmationTokenProvider;
        private readonly PasswordResetTokenProvider<ApplicationUser> _passwordResetTokenProvider;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IImageStorageService imageStorage,
            IEmailService emailService,
            EmailConfirmationTokenProvider<ApplicationUser> confirmationTokenProvider,
            PasswordResetTokenProvider<ApplicationUser> passwordResetTokenProvider,
            IMemoryCache cache,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _imageStorage = imageStorage;
            _emailService = emailService;
            _confirmationTokenProvider = confirmationTokenProvider;
            _passwordResetTokenProvider = passwordResetTokenProvider;
            _cache = cache;
            _logger = logger;
        }

        // ───────────────────────────── Registro ─────────────────────────────

        public async Task<ServiceResult> RegisterAsync(RegisterRequestDto request, string activationUrl)
        {
            var userName = request.UserName.Trim();
            var email = request.Email.Trim();

            // FindByName/FindByEmail comparan sin distinguir mayúsculas (usan los campos normalizados)
            if (await _userManager.FindByNameAsync(userName) is not null)
                return ServiceResult.Failure(nameof(request.UserName), "Este nombre de usuario ya se encuentra registrado.");

            if (await _userManager.FindByEmailAsync(email) is not null)
                return ServiceResult.Failure(nameof(request.Email), "Este correo electrónico ya se encuentra registrado.");

            var rutaFoto = await _imageStorage.SaveAsync(request.FotoPerfil, CarpetaFotos);

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = request.Telefono.Trim(),
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                FotoPerfil = rutaFoto,
                EmailConfirmed = false, // la cuenta nace inactiva
                FechaRegistro = DateTime.UtcNow
            };

            var resultado = await _userManager.CreateAsync(user, request.Password);
            if (!resultado.Succeeded)
            {
                // Si no se creó el usuario, la foto no debe quedar huérfana
                await _imageStorage.DeleteAsync(rutaFoto);
                return MapIdentityErrors(resultado);
            }

            await SendActivationEmailAsync(user, activationUrl);
            return ServiceResult.Success();
        }

        // ───────────────────────────── Activación ─────────────────────────────

        public async Task<ConfirmEmailStatus> ConfirmEmailAsync(string? userId, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return ConfirmEmailStatus.InvalidOrUsed;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.EmailConfirmed)
                return ConfirmEmailStatus.InvalidOrUsed;

            string tokenDecodificado;
            try
            {
                tokenDecodificado = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return ConfirmEmailStatus.InvalidOrUsed;
            }

            var resultado = await _userManager.ConfirmEmailAsync(user, tokenDecodificado);
            if (resultado.Succeeded)
            {
                // Cambiar el sello de seguridad invalida el token: queda de un solo uso
                await _userManager.UpdateSecurityStampAsync(user);
                return ConfirmEmailStatus.Activated;
            }

            return _confirmationTokenProvider.IsExpired(tokenDecodificado)
                ? ConfirmEmailStatus.Expired
                : ConfirmEmailStatus.InvalidOrUsed;
        }

        public async Task<ResendActivationStatus> ResendActivationAsync(string userName, string activationUrl)
        {
            userName = userName.Trim();

            // Una solicitud cada 5 minutos por nombre de usuario. Se aplica a cualquier nombre,
            // exista o no, para no revelar qué cuentas están registradas.
            var claveCache = $"reenvio-activacion:{userName.ToUpperInvariant()}";
            if (_cache.TryGetValue(claveCache, out _))
                return ResendActivationStatus.Throttled;

            _cache.Set(claveCache, true, EsperaReenvio);

            var user = await _userManager.FindByNameAsync(userName);

            // Si no existe o ya está activa: no se envía nada ni se cambia su estado
            if (user is null || user.EmailConfirmed)
                return ResendActivationStatus.Processed;

            // Cambiar el sello de seguridad invalida los enlaces de activación anteriores
            await _userManager.UpdateSecurityStampAsync(user);
            await SendActivationEmailAsync(user, activationUrl);

            return ResendActivationStatus.Processed;
        }

        // ───────────────────────────── Inicio y cierre de sesión ─────────────────────────────

        public async Task<LoginStatus> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName.Trim());

            // Usuario inexistente: mismo mensaje que contraseña incorrecta, y no se cuenta como intento
            if (user is null)
                return LoginStatus.InvalidCredentials;

            // Bloqueada: se rechaza aunque la contraseña sea correcta, sin alargar el bloqueo
            if (await _userManager.IsLockedOutAsync(user))
                return LoginStatus.LockedOut;

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                // Suma un intento fallido; al llegar a 5 bloquea la cuenta 15 minutos
                await _userManager.AccessFailedAsync(user);
                return await _userManager.IsLockedOutAsync(user)
                    ? LoginStatus.LockedOut
                    : LoginStatus.InvalidCredentials;
            }

            // La cuenta inactiva solo se informa cuando las credenciales son correctas
            if (!user.EmailConfirmed)
                return LoginStatus.Inactive;

            // Enunciado: un inicio de sesión exitoso reinicia los intentos fallidos
            await _userManager.ResetAccessFailedCountAsync(user);

            if (request.RememberMe)
            {
                // "Mantener sesión iniciada": máximo 7 días, sin renovarse (AllowRefresh = false)
                var propiedades = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(DuracionSesionPersistente),
                    AllowRefresh = false
                };
                await _signInManager.SignInAsync(user, propiedades);
            }
            else
            {
                // Cookie de sesión: se cierra con el navegador o tras 30 min de inactividad
                await _signInManager.SignInAsync(user, isPersistent: false);
            }

            return LoginStatus.Succeeded;
        }

        public Task LogoutAsync() => _signInManager.SignOutAsync();

        // ───────────────────────────── Restablecimiento de contraseña ─────────────────────────────

        public async Task ForgotPasswordAsync(string userName, string resetUrl)
        {
            var user = await _userManager.FindByNameAsync(userName.Trim());

            // Si no existe no se hace nada; el controlador muestra el mismo mensaje en ambos casos.
            // Enunciado: solicitar el restablecimiento no activa, desactiva ni desbloquea la cuenta.
            if (user is null)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Enunciado: generar un enlace nuevo invalida el anterior. Se guarda la huella del último
            // token; al restablecer solo se acepta el que coincide con ella.
            await _userManager.SetAuthenticationTokenAsync(user, ProveedorTokenPropio, NombreTokenRestablecimiento, Huella(token));

            var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var enlace = $"{resetUrl}?userId={Uri.EscapeDataString(user.Id)}&token={tokenCodificado}";

            var cuerpo = $"""
                <p>Hola {HtmlEncoder.Default.Encode(user.Nombre)},</p>
                <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta de LinkUp Pro.
                   Para crear una nueva, haz clic en el siguiente enlace. Es válido por 1 hora y solo puede usarse una vez.</p>
                <p><a href="{HtmlEncoder.Default.Encode(enlace)}">Restablecer mi contraseña</a></p>
                <p>Si no solicitaste este cambio, ignora este mensaje: tu contraseña actual seguirá funcionando.</p>
                """;

            try
            {
                await _emailService.SendAsync(user.Email!, "Restablece tu contraseña de LinkUp Pro", cuerpo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de restablecimiento al usuario {UserId}.", user.Id);
            }
        }

        public async Task<ResetPasswordStatus> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user is null)
                return ResetPasswordStatus.InvalidOrUsed;

            string tokenDecodificado;
            try
            {
                tokenDecodificado = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (FormatException)
            {
                return ResetPasswordStatus.InvalidOrUsed;
            }

            if (_passwordResetTokenProvider.IsExpired(tokenDecodificado))
                return ResetPasswordStatus.Expired;

            // Solo se acepta el último enlace generado para este usuario
            var huellaGuardada = await _userManager.GetAuthenticationTokenAsync(user, ProveedorTokenPropio, NombreTokenRestablecimiento);
            if (huellaGuardada is null || huellaGuardada != Huella(tokenDecodificado))
                return ResetPasswordStatus.InvalidOrUsed;

            // ResetPasswordAsync valida el token y cambia el sello de seguridad, lo que:
            // - deja el token de un solo uso
            // - invalida todas las sesiones anteriores, incluidas las de "Mantener sesión iniciada"
            var resultado = await _userManager.ResetPasswordAsync(user, tokenDecodificado, request.NewPassword);
            if (!resultado.Succeeded)
            {
                return resultado.Errors.Any(e => e.Code == "InvalidToken")
                    ? ResetPasswordStatus.InvalidOrUsed
                    : ResetPasswordStatus.PasswordRejected;
            }

            await _userManager.RemoveAuthenticationTokenAsync(user, ProveedorTokenPropio, NombreTokenRestablecimiento);

            // Enunciado: reinicia los intentos fallidos y elimina el bloqueo.
            // No toca EmailConfirmed: una cuenta inactiva sigue inactiva.
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            return ResetPasswordStatus.Succeeded;
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        private async Task SendActivationEmailAsync(ApplicationUser user, string activationUrl)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var enlace = $"{activationUrl}?userId={Uri.EscapeDataString(user.Id)}&token={tokenCodificado}";

            var cuerpo = $"""
                <p>Hola {HtmlEncoder.Default.Encode(user.Nombre)},</p>
                <p>Gracias por registrarte en LinkUp Pro. Para activar tu cuenta, haz clic en el siguiente enlace.
                   Es válido por 24 horas y solo puede usarse una vez.</p>
                <p><a href="{HtmlEncoder.Default.Encode(enlace)}">Activar mi cuenta</a></p>
                <p>Si no creaste esta cuenta, ignora este mensaje.</p>
                """;

            try
            {
                await _emailService.SendAsync(user.Email!, "Activa tu cuenta de LinkUp Pro", cuerpo);
            }
            catch (Exception ex)
            {
                // No se muestran detalles al usuario; podrá pedir un nuevo enlace con "Reenviar correo de activación"
                _logger.LogError(ex, "No se pudo enviar el correo de activación al usuario {UserId}.", user.Id);
            }
        }

        // Huella SHA-256 del token: así en la base de datos no queda el token en claro
        private static string Huella(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static ServiceResult MapIdentityErrors(IdentityResult resultado)
        {
            var result = new ServiceResult { Succeeded = false };
            foreach (var error in resultado.Errors)
            {
                result.Errors.Add(error.Code switch
                {
                    "DuplicateUserName" => new ServiceError("UserName", "Este nombre de usuario ya se encuentra registrado."),
                    "DuplicateEmail" => new ServiceError("Email", "Este correo electrónico ya se encuentra registrado."),
                    "InvalidUserName" => new ServiceError("UserName", "El nombre de usuario contiene caracteres no permitidos."),
                    _ when error.Code.StartsWith("Password") => new ServiceError("Password", "La contraseña no cumple las reglas de seguridad."),
                    _ => new ServiceError("", "No fue posible crear la cuenta. Verifique los datos e inténtelo nuevamente.")
                });
            }
            return result;
        }
    }
}
