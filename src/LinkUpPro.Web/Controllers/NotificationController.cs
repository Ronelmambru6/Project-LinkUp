using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Notifications;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;

        public NotificationController(INotificationService notificationService, IMapper mapper)
        {
            _notificationService = notificationService;
            _mapper = mapper;
        }

        // El Id sale siempre de la sesión: cada usuario solo ve y modifica sus notificaciones
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var notificaciones = await _notificationService.GetMineAsync(UserId);

            return View(new NotificationsIndexViewModel
            {
                Notificaciones = notificaciones.Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Actor = _mapper.Map<UserSummaryViewModel>(n.Actor),
                    Tipo = n.Tipo,
                    Descripcion = n.Descripcion,
                    Fecha = n.Fecha,
                    Leida = n.Leida
                }).ToList()
            });
        }

        // Enlace al contenido: si fue eliminado o ya no hay permiso, se muestra su mensaje aquí mismo
        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var destino = await _notificationService.ResolveTargetAsync(id, UserId);
            if (!destino.Succeeded || destino.Data is null)
            {
                TempData["Error"] = destino.FirstError;
                return RedirectToAction(nameof(Index));
            }

            var fragmento = destino.Data.CommentId is int commentId ? $"comentario-{commentId}" : null;
            return RedirectToAction("Details", "Post", new { id = destino.Data.PostId }, fragmento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var resultado = await _notificationService.MarkAsReadAsync(id, UserId);

            if (resultado.Succeeded)
                TempData["Success"] = "La notificación fue marcada como leída.";
            else
                TempData["Error"] = resultado.FirstError;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(UserId);
            TempData["Success"] = "Todas las notificaciones fueron marcadas como leídas.";
            return RedirectToAction(nameof(Index));
        }
    }
}
