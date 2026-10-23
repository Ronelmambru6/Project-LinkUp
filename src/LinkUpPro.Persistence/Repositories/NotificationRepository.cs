using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<List<Notification>> GetByRecipientAsync(string userId) =>
            _dbSet.AsNoTracking()
                .Where(n => n.DestinatarioId == userId)
                .OrderByDescending(n => n.Fecha)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

        public Task<int> CountUnreadAsync(string userId) =>
            _dbSet.CountAsync(n => n.DestinatarioId == userId && !n.Leida);

        public Task<List<Notification>> GetUnreadAsync(string userId) =>
            _dbSet.Where(n => n.DestinatarioId == userId && !n.Leida).ToListAsync();
    }
}
