using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Comments;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Comments;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly IInteractionService _interactionService;
        private readonly IMapper _mapper;

        public CommentController(IInteractionService interactionService, IMapper mapper)
        {
            _interactionService = interactionService;
            _mapper = mapper;
        }

        // El Id del usuario sale siempre de la sesión, nunca del formulario
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ───────────────────────────── Comentar y responder ─────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int postId, int? parentCommentId, string? contenido)
        {
            var resultado = await _interactionService.AddCommentAsync(postId, UserId, new SaveCommentRequestDto
            {
                Contenido = contenido ?? string.Empty,
                ParentCommentId = parentCommentId
            });

            if (resultado.Succeeded)
                TempData["Success"] = parentCommentId is null
                    ? "El comentario fue publicado correctamente."
                    : "La respuesta fue publicada correctamente.";
            else
                TempData["Error"] = resultado.FirstError;

            return VolverAPublicacion(postId, resultado.Succeeded);
        }

        // ───────────────────────────── Editar ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var comentario = await _interactionService.GetCommentForEditAsync(id, UserId);
            if (!comentario.Succeeded || comentario.Data is null)
                return VolverAlInicio(comentario.FirstError);

            return View(new EditCommentViewModel
            {
                Id = comentario.Data.Id,
                PostId = comentario.Data.PostId,
                Contenido = comentario.Data.Contenido
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCommentViewModel model)
        {
            // La publicación se toma de la base de datos, no del formulario
            var comentario = await _interactionService.GetCommentForEditAsync(id, UserId);
            if (!comentario.Succeeded || comentario.Data is null)
                return VolverAlInicio(comentario.FirstError);

            model.Id = id;
            model.PostId = comentario.Data.PostId;

            if (ModelState.IsValid)
            {
                var resultado = await _interactionService.UpdateCommentAsync(id, UserId, model.Contenido);
                if (resultado.Succeeded)
                {
                    TempData["Success"] = "El comentario fue actualizado correctamente.";
                    return VolverAPublicacion(model.PostId, true);
                }

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Field, error.Message);
            }

            return View(model);
        }

        // ───────────────────────────── Eliminar ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var comentario = await _interactionService.GetCommentForDeleteAsync(id, UserId);
            if (!comentario.Succeeded || comentario.Data is null)
                return VolverAlInicio(comentario.FirstError);

            return View(new DeleteCommentViewModel
            {
                Id = comentario.Data.Id,
                PostId = comentario.Data.PostId,
                Autor = _mapper.Map<UserSummaryViewModel>(comentario.Data.Autor),
                Contenido = comentario.Data.Contenido,
                FechaCreacion = comentario.Data.FechaCreacion
            });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comentario = await _interactionService.GetCommentForDeleteAsync(id, UserId);
            if (!comentario.Succeeded || comentario.Data is null)
                return VolverAlInicio(comentario.FirstError);

            var postId = comentario.Data.PostId;
            var resultado = await _interactionService.DeleteCommentAsync(id, UserId);

            if (resultado.Succeeded)
                TempData["Success"] = "El comentario fue eliminado correctamente.";
            else
                TempData["Error"] = resultado.FirstError;

            return VolverAPublicacion(postId, resultado.Succeeded);
        }

        // ───────────────────────────── Ayudas ─────────────────────────────

        private IActionResult VolverAPublicacion(int postId, bool exito) =>
            RedirectToAction("Details", "Post", new { id = postId }, exito ? "comentarios" : null);

        private IActionResult VolverAlInicio(string mensaje)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction("Index", "Home");
        }
    }
}
