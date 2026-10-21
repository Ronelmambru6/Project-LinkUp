using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Interfaces.Repositories
{
    // Consultas de usuarios. Los usuarios viven en Identity (Persistence), por eso
    // Application solo los conoce a través de esta interfaz y de UserSummaryDto.
    // "Activo" = cuenta activada por correo.
    public interface IUserQueryRepository
    {
        Task<UserSummaryDto?> GetActiveByIdAsync(string userId);
        Task<bool> IsActiveAsync(string userId);
        Task<List<UserSummaryDto>> GetActiveByIdsAsync(IEnumerable<string> userIds);

        // Usuarios activos cuyo nombre de usuario contiene el texto (sin distinguir mayúsculas),
        // excluyendo los Ids indicados. Ordenados por nombre de usuario.
        Task<List<UserSummaryDto>> SearchActiveAsync(string? userName, ICollection<string> excludedIds);
    }
}
