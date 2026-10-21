using System.Security.Claims;
using LinkUpPro.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.ViewComponents
{
    // Contador de solicitudes recibidas pendientes en el menú.
    // Se calcula cada vez que se carga una pantalla (no requiere tiempo real).
    public class PendingRequestsCounterViewComponent : ViewComponent
    {
        private readonly IFriendRequestService _friendRequestService;

        public PendingRequestsCounterViewComponent(IFriendRequestService friendRequestService)
        {
            _friendRequestService = friendRequestService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var cantidad = userId is null ? 0 : await _friendRequestService.CountPendingReceivedAsync(userId);
            return View(cantidad);
        }
    }
}
