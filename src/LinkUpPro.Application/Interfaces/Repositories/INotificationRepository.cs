using LinkUpPro.Domain.Entities;

namespace LinkUpPro.Application.Interfaces.Repositories
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        // Notificaciones del destinatario, de la más reciente a la más antigua
        Task<List<Notification>> GetByRecipientAsync(string userId);

        Task<int> CountUnreadAsync(string userId);

        // Con seguimiento de cambios, para marcarlas como leídas
        Task<List<Notification>> GetUnreadAsync(string userId);
    }
}
