using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Dtos.Comments;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Comments;
using LinkUpPro.Application.ViewModels.Posts;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly IInteractionService _interactionService;
        private readonly IMapper _mapper;

        public PostController(IPostService postService, IInteractionService interactionService, IMapper mapper)
        {
            _postService = postService;
            _interactionService = interactionService;
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

            var post = _mapper.Map<PostViewModel>(resultado.Data);
            post.EsAutor = post.AutorId == UserId;

            // El servicio vuelve a validar el acceso a la publicación en cada consulta
            var comentarios = await _interactionService.GetCommentsAsync(id, UserId);
            if (!comentarios.Succeeded || comentarios.Data is null)
                return VolverConError(comentarios.FirstError);

            var reaccion = await _interactionService.GetMyReactionAsync(id, UserId);

            return View(new PostDetailsViewModel
            {
                Post = post,
                MiReaccion = reaccion.Succeeded ? reaccion.Data : null,
                Comentarios = comentarios.Data.Select(c => ConstruirComentario(c, 0, post.PermitirComentarios)).ToList()
            });
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

        // Convierte el árbol de comentarios y decide qué acciones tiene cada uno para el usuario actual
        private CommentViewModel ConstruirComentario(CommentDto dto, int nivel, bool permitirComentarios)
        {
            var esAutor = dto.AutorId == UserId;
            return new CommentViewModel
            {
                Id = dto.Id,
                PostId = dto.PostId,
                Autor = _mapper.Map<UserSummaryViewModel>(dto.Autor),
                Contenido = dto.Contenido,
                FechaCreacion = dto.FechaCreacion,
                FechaModificacion = dto.FechaModificacion,
                Eliminado = dto.IsDeleted,
                EsAutor = esAutor,
                PuedeVerPerfil = dto.PuedeVerPerfil,
                Nivel = nivel,
                // Con los comentarios desactivados no hay formulario, Responder ni Editar; Eliminar sí se mantiene
                PuedeResponder = permitirComentarios && !dto.IsDeleted,
                PuedeEditar = permitirComentarios && !dto.IsDeleted && esAutor,
                PuedeEliminar = !dto.IsDeleted && esAutor,
                Respuestas = dto.Respuestas.Select(r => ConstruirComentario(r, nivel + 1, permitirComentarios)).ToList()
            };
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
