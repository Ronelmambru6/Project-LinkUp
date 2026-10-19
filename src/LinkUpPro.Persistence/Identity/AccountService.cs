using System.Text;
using System.Text.Encodings.Web;
using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Persistence.Identity
{
    // Vive en Persistence porque usa UserManager<ApplicationUser>, que depende de Identity.
    // La interfaz está en Application, así Web no conoce los detalles de Identity.
    public class AccountService : IAccountService
    {
        private const string CarpetaFotos = "profiles";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStorageService _imageStorage;
        private readonly IEmailService _emailService;
        private readonly EmailConfirmationTokenProvider<ApplicationUser> _confirmationTokenProvider;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            IImageStorageService imageStorage,
            IEmailService emailService,
            EmailConfirmationTokenProvider<ApplicationUser> confirmationTokenProvider,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _imageStorage = imageStorage;
            _emailService = emailService;
            _confirmationTokenProvider = confirmationTokenProvider;
            _logger = logger;
        }

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

        // Se reutilizará en el reenvío de activación (PR B)
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
