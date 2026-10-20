using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;
using LinkUpPro.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class FriendshipRepository : GenericRepository<Friendship>, IFriendshipRepository
    {
        public FriendshipRepository(ApplicationDbContext context) : base(context)
        {
        }

        public Task<Friendship?> GetByPairAsync(string usuarioAId, string usuarioBId) =>
            _dbSet.FirstOrDefaultAsync(f => f.UsuarioAId == usuarioAId && f.UsuarioBId == usuarioBId);

        public async Task<List<string>> GetActiveFriendIdsAsync(string userId)
        {
            var usuariosActivos = _context.Users.Where(u => u.EmailConfirmed).Select(u => u.Id);

            return await _dbSet.AsNoTracking()
                .Where(f => f.Estado == EstadoAmistad.Activa && (f.UsuarioAId == userId || f.UsuarioBId == userId))
                .Select(f => f.UsuarioAId == userId ? f.UsuarioBId : f.UsuarioAId)
                .Where(amigoId => usuariosActivos.Contains(amigoId))
                .ToListAsync();
        }
    }
}
