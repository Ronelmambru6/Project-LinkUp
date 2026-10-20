using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;
using LinkUpPro.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class FriendRequestRepository : GenericRepository<FriendRequest>, IFriendRequestRepository
    {
        public FriendRequestRepository(ApplicationDbContext context) : base(context)
        {
        }

        private IQueryable<string> UsuariosActivos => _context.Users.Where(u => u.EmailConfirmed).Select(u => u.Id);

        public Task<List<FriendRequest>> GetPendingBetweenAsync(string userId1, string userId2) =>
            _dbSet.Where(r => r.Estado == EstadoSolicitud.EnEspera &&
                              ((r.EmisorId == userId1 && r.ReceptorId == userId2) ||
                               (r.EmisorId == userId2 && r.ReceptorId == userId1)))
                  .ToListAsync();

        public Task<List<FriendRequest>> GetPendingReceivedFromActiveAsync(string receptorId)
        {
            var activos = UsuariosActivos;
            return _dbSet.AsNoTracking()
                .Where(r => r.ReceptorId == receptorId && r.Estado == EstadoSolicitud.EnEspera && activos.Contains(r.EmisorId))
                .OrderByDescending(r => r.FechaEnvio)
                .ToListAsync();
        }

        public Task<List<FriendRequest>> GetSentVisibleToActiveAsync(string emisorId)
        {
            var activos = UsuariosActivos;
            return _dbSet.AsNoTracking()
                .Where(r => r.EmisorId == emisorId
                            && r.Estado != EstadoSolicitud.Cancelada
                            && !r.OcultaParaEmisor
                            && activos.Contains(r.ReceptorId))
                .OrderByDescending(r => r.FechaEnvio)
                .ToListAsync();
        }

        public Task<int> CountPendingReceivedFromActiveAsync(string receptorId)
        {
            var activos = UsuariosActivos;
            return _dbSet.CountAsync(r => r.ReceptorId == receptorId
                                          && r.Estado == EstadoSolicitud.EnEspera
                                          && activos.Contains(r.EmisorId));
        }

        public Task<List<string>> GetPendingCounterpartIdsAsync(string userId) =>
            _dbSet.AsNoTracking()
                .Where(r => r.Estado == EstadoSolicitud.EnEspera && (r.EmisorId == userId || r.ReceptorId == userId))
                .Select(r => r.EmisorId == userId ? r.ReceptorId : r.EmisorId)
                .ToListAsync();
    }
}
