using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Profile;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        // Misma cookie que usa AccountController para detectar el cierre por inactividad.
        // Se borra al cerrar la sesión para que el login no muestre ese mensaje por error.
        private const string MarcaSesion = "LinkUpPro.SesionActiva";

        private readonly IProfileService _profileService;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public ProfileController(IProfileService profileService, IAccountService accountService, IMapper mapper)
        {
            _profileService = profileService;
            _accountService = accountService;
            _mapper = mapper;
        }

        // El Id sale siempre de la sesión: no hay forma de pedir el perfil de otra persona
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var perfil = await _profileService.GetAsync(UserId);
            if (perfil is null)
                return RedirectToAction("Login", "Account");

            return View(_mapper.Map<SaveProfileViewModel>(perfil));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SaveProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _profileService.UpdateAsync(UserId, _mapper.Map<UpdateProfileRequestDto>(model));

                if (resultado.Succeeded)
                {
                    if (resultado.Data)
                    {
                        // Cambió la contraseña: se cierra la sesión actual y se vuelve al login
                        await _accountService.LogoutAsync();
                        Response.Cookies.Delete(MarcaSesion);
                        TempData["Success"] = "Su perfil y contraseña fueron actualizados correctamente. Inicie sesión nuevamente.";
                        return RedirectToAction("Login", "Account");
                    }

                    TempData["Success"] = "Su perfil fue actualizado correctamente.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Field, error.Message);
            }

            await RecargarDatosDeSoloLecturaAsync(model);
            return View(model);
        }

        // Usuario, correo y foto actual se vuelven a leer de la base de datos: nunca del formulario
        private async Task RecargarDatosDeSoloLecturaAsync(SaveProfileViewModel model)
        {
            var perfil = await _profileService.GetAsync(UserId);
            if (perfil is null)
                return;

            model.UserName = perfil.UserName;
            model.Email = perfil.Email;
            model.FotoActual = perfil.FotoPerfil;
        }
    }
}
