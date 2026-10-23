using System.Security.Claims;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class ReactionController : Controller
    {
        private readonly IInteractionService _interactionService;

        public ReactionController(IInteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Una reacción por usuario: si ya existe, se actualiza la existente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(int postId, TipoReaccion tipo)
        {
            var resultado = await _interactionService.SetReactionAsync(postId, UserId, tipo);
            return Volver(postId, resultado, "Su reacción fue registrada correctamente.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int postId)
        {
            var resultado = await _interactionService.RemoveReactionAsync(postId, UserId);
            return Volver(postId, resultado, "Su reacción fue eliminada correctamente.");
        }

        private IActionResult Volver(int postId, ServiceResult resultado, string mensajeExito)
        {
            if (resultado.Succeeded)
            {
                TempData["Success"] = mensajeExito;
                return RedirectToAction("Details", "Post", new { id = postId }, "reacciones");
            }

            // Publicación eliminada, sin permiso, etc.: no tiene sentido volver a ella
            TempData["Error"] = resultado.FirstError;
            return RedirectToAction("Index", "Home");
        }
    }
}
