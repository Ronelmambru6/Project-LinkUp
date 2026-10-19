using AutoMapper;
using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    // Enunciado: [Authorize] en el controlador y [AllowAnonymous] solo en las acciones públicas
    // (nunca [AllowAnonymous] en un controlador que también tiene acciones privadas, como Logout).
    [Authorize]
    public class AccountController : Controller
    {
        // Cookie de sesión (sin fecha de vencimiento) que indica que hubo un login sin "Mantener sesión".
        // Si al llegar al login la cookie de autenticación ya no existe pero esta sí, la sesión venció por inactividad.
        private const string MarcaSesion = "LinkUpPro.SesionActiva";

        private const string MensajeInactividad = "Su sesión finalizó por inactividad. Inicie sesión nuevamente para continuar.";
        private const string MensajeDebeIniciarSesion = "Debe iniciar sesión para acceder a esta sección.";

        private readonly IAccountService _accountService;
        private readonly IImageStorageService _imageStorage;
        private readonly IMapper _mapper;

        public AccountController(IAccountService accountService, IImageStorageService imageStorage, IMapper mapper)
        {
            _accountService = accountService;
            _imageStorage = imageStorage;
            _mapper = mapper;
        }

        private bool EstaAutenticado => User.Identity?.IsAuthenticated == true;

        // ───────────────────────────── Login ─────────────────────────────

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            var habiaSesion = Request.Cookies.ContainsKey(MarcaSesion);
            if (habiaSesion)
            {
                Response.Cookies.Delete(MarcaSesion);
                ViewData["Aviso"] = MensajeInactividad;
            }
            else if (!string.IsNullOrEmpty(returnUrl) && returnUrl != "/")
            {
                // Intentó entrar a una pantalla protegida sin haber iniciado sesión
                ViewData["Aviso"] = MensajeDebeIniciarSesion;
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(model);

            var estado = await _accountService.LoginAsync(_mapper.Map<LoginRequestDto>(model));

            switch (estado)
            {
                case LoginStatus.Succeeded:
                    if (!model.RememberMe)
                    {
                        Response.Cookies.Append(MarcaSesion, "1", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true
                        });
                    }

                    // Solo se redirige a direcciones internas (evita redirecciones a sitios externos)
                    return Url.IsLocalUrl(model.ReturnUrl)
                        ? LocalRedirect(model.ReturnUrl!)
                        : RedirectToAction("Index", "Home");

                case LoginStatus.LockedOut:
                    ModelState.AddModelError(string.Empty,
                        "La cuenta se encuentra bloqueada temporalmente debido a varios intentos fallidos. Inténtelo nuevamente en 15 minutos o restablezca su contraseña.");
                    break;

                case LoginStatus.Inactive:
                    ModelState.AddModelError(string.Empty,
                        "Su cuenta se encuentra inactiva. Debe activarla mediante el enlace enviado a su correo electrónico.");
                    break;

                default:
                    ModelState.AddModelError(string.Empty, "El nombre de usuario o la contraseña son incorrectos.");
                    break;
            }

            return View(model);
        }

        // Enunciado: cerrar sesión por POST protegido, nunca por un enlace GET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            Response.Cookies.Delete(MarcaSesion);
            return RedirectToAction(nameof(Login));
        }

        // ───────────────────────────── Registro ─────────────────────────────

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            return View(new SaveRegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SaveRegisterViewModel model)
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            if (model.FotoPerfil is not null && !_imageStorage.IsValidImage(model.FotoPerfil))
                ModelState.AddModelError(nameof(model.FotoPerfil),
                    "La foto de perfil debe ser una imagen válida (.jpg, .jpeg, .png o .webp) de máximo 5 MB.");

            if (!ModelState.IsValid)
                return View(model);

            var activationUrl = Url.Action(nameof(ConfirmEmail), "Account", null, Request.Scheme)!;
            var resultado = await _accountService.RegisterAsync(_mapper.Map<RegisterRequestDto>(model), activationUrl);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Field, error.Message);

                return View(model);
            }

            return RedirectToAction(nameof(RegisterConfirmation));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult RegisterConfirmation() => View();

        // ───────────────────────────── Activación ─────────────────────────────

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            var estado = await _accountService.ConfirmEmailAsync(userId, token);

            if (estado == ConfirmEmailStatus.Activated)
            {
                TempData["Success"] = "Su cuenta fue activada correctamente. Ya puede iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }

            ViewData["Mensaje"] = estado == ConfirmEmailStatus.Expired
                ? "El enlace de activación ha vencido. Solicite uno nuevo."
                : "El enlace de activación no es válido o ya fue utilizado.";

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResendActivation()
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            return View(new ResendActivationViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivation(ResendActivationViewModel model)
        {
            if (EstaAutenticado)
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(model);

            var activationUrl = Url.Action(nameof(ConfirmEmail), "Account", null, Request.Scheme)!;
            var estado = await _accountService.ResendActivationAsync(model.UserName, activationUrl);

            if (estado == ResendActivationStatus.Throttled)
            {
                ModelState.AddModelError(string.Empty,
                    "Ya se procesó una solicitud reciente para este usuario. Espere 5 minutos antes de solicitar un nuevo enlace.");
                return View(model);
            }

            // Mismo mensaje exista o no la cuenta, para no revelar qué usuarios están registrados
            TempData["Success"] = "Si la cuenta existe y todavía no ha sido activada, recibirá un nuevo enlace de activación.";
            return RedirectToAction(nameof(Login));
        }
    }
}
