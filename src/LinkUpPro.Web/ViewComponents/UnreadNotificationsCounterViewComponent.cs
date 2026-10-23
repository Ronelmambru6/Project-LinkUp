using System.Security.Claims;
using LinkUpPro.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.ViewComponents
{
    // Contador de notificaciones no leídas en el menú.
    // Se calcula al cargar cada pantalla: no hay tiempo real (sin AJAX ni WebSockets).
    public class UnreadNotificationsCounterViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public UnreadNotificationsCounterViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var cantidad = userId is null ? 0 : await _notificationService.CountUnreadAsync(userId);
            return View(cantidad);
        }
    }
}
