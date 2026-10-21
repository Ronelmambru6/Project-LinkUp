using System.Linq.Expressions;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Persistence.Contexts;
using LinkUpPro.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class UserQueryRepository : IUserQueryRepository
    {
        private static readonly Expression<Func<ApplicationUser, UserSummaryDto>> AResumen = u => new UserSummaryDto
        {
            Id = u.Id,
            UserName = u.UserName!,
            Nombre = u.Nombre,
            Apellido = u.Apellido,
            FotoPerfil = u.FotoPerfil
        };

        private readonly ApplicationDbContext _context;

        public UserQueryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Cuenta activa = activada por correo
        private IQueryable<ApplicationUser> Activos => _context.Users.AsNoTracking().Where(u => u.EmailConfirmed);

        public Task<UserSummaryDto?> GetActiveByIdAsync(string userId) =>
            Activos.Where(u => u.Id == userId).Select(AResumen).FirstOrDefaultAsync();

        public Task<bool> IsActiveAsync(string userId) =>
            Activos.AnyAsync(u => u.Id == userId);

        public async Task<List<UserSummaryDto>> GetActiveByIdsAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new List<UserSummaryDto>();

            return await Activos
                .Where(u => ids.Contains(u.Id))
                .OrderBy(u => u.Nombre).ThenBy(u => u.Apellido)
                .Select(AResumen)
                .ToListAsync();
        }

        public async Task<List<UserSummaryDto>> SearchActiveAsync(string? userName, ICollection<string> excludedIds)
        {
            var excluidos = excludedIds.ToList();
            var consulta = Activos.Where(u => !excluidos.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(userName))
            {
                // NormalizedUserName está en mayúsculas: así la búsqueda no distingue mayúsculas
                var filtro = userName.Trim().ToUpperInvariant();
                consulta = consulta.Where(u => u.NormalizedUserName!.Contains(filtro));
            }

            return await consulta.OrderBy(u => u.UserName).Select(AResumen).ToListAsync();
        }
    }
}
