using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Notifications;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Notificaciones de interacción (comentarios, respuestas y reacciones). Se generan en
    // InteractionService; aquí solo se consultan y se marcan como leídas.
    // Cada usuario solo ve y modifica las suyas: el Id siempre llega de la sesión.
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetMineAsync(string userId);
        Task<int> CountUnreadAsync(string userId);

        Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId);
        Task<ServiceResult> MarkAllAsReadAsync(string userId);

        // Comprueba que el contenido siga disponible y que el usuario todavía pueda verlo
        Task<ServiceResult<NotificationTargetDto>> ResolveTargetAsync(int notificationId, string userId);
    }
}
