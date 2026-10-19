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
        private readonly IAccountService _accountService;
        private readonly IImageStorageService _imageStorage;
        private readonly IMapper _mapper;

        public AccountController(IAccountService accountService, IImageStorageService imageStorage, IMapper mapper)
        {
            _accountService = accountService;
            _imageStorage = imageStorage;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new SaveRegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SaveRegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
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

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
        {
            var estado = await _accountService.ConfirmEmailAsync(userId, token);

            if (estado == ConfirmEmailStatus.Activated)
            {
                TempData["Success"] = "Su cuenta fue activada correctamente. Ya puede iniciar sesión.";
                return RedirectToAction("Login"); // la acción Login se crea en el PR B
            }

            ViewData["Mensaje"] = estado == ConfirmEmailStatus.Expired
                ? "El enlace de activación ha vencido. Solicite uno nuevo."
                : "El enlace de activación no es válido o ya fue utilizado.";

            return View();
        }
    }
}
