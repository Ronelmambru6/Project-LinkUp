using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMapper _mapper;

        public PostController(IPostService postService, IMapper mapper)
        {
            _postService = postService;
            _mapper = mapper;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Acceso directo por URL: el servicio valida existencia y privacidad
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var resultado = await _postService.GetForViewAsync(id, UserId);
            if (!resultado.Succeeded || resultado.Data is null)
                return VolverConError(resultado.FirstError);

            var vm = _mapper.Map<PostViewModel>(resultado.Data);
            vm.EsAutor = vm.AutorId == UserId;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resultado = await _postService.GetForEditAsync(id, UserId);
            if (!resultado.Succeeded || resultado.Data is null)
                return VolverConError(resultado.FirstError);

            return View(_mapper.Map<SavePostViewModel>(resultado.Data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SavePostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _postService.UpdateAsync(id, UserId, _mapper.Map<SavePostRequestDto>(model));
                if (resultado.Succeeded)
                {
                    TempData["Success"] = "La publicación fue actualizada correctamente.";
                    return RedirectToAction("Index", "Home");
                }

                // Sin permiso o publicación inexistente: no tiene sentido volver al formulario
                if (resultado.Errors.Any(e => string.IsNullOrEmpty(e.Field) && !EsErrorDeFormulario(e.Message)))
                    return VolverConError(resultado.FirstError);

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Field, error.Message);
            }

            // Se recarga la imagen actual desde la base de datos (el formulario no la envía)
            var actual = await _postService.GetForEditAsync(id, UserId);
            if (!actual.Succeeded || actual.Data is null)
                return VolverConError(actual.FirstError);

            model.Id = id;
            model.RutaImagenActual = actual.Data.RutaImagen;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var resultado = await _postService.GetForDeleteAsync(id, UserId);
            if (!resultado.Succeeded || resultado.Data is null)
                return VolverConError(resultado.FirstError);

            var vm = _mapper.Map<PostViewModel>(resultado.Data);
            vm.EsAutor = true;
            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resultado = await _postService.DeleteAsync(id, UserId);
            if (resultado.Succeeded)
                TempData["Success"] = "La publicación fue eliminada correctamente.";
            else
                TempData["Error"] = resultado.FirstError;

            return RedirectToAction("Index", "Home");
        }

        private IActionResult VolverConError(string mensaje)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction("Index", "Home");
        }

        // Errores generales de validación del formulario (se muestran en el formulario, no redirigen)
        private static bool EsErrorDeFormulario(string mensaje) =>
            !mensaje.StartsWith("No posee permisos") && !mensaje.StartsWith("Esta publicación");
    }
}
