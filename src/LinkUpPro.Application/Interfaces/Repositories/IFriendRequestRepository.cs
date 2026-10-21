using LinkUpPro.Domain.Entities;

namespace LinkUpPro.Application.Interfaces.Repositories
{
    public interface IFriendRequestRepository : IGenericRepository<FriendRequest>
    {
        // Solicitudes en espera entre dos usuarios, en cualquier dirección
        Task<List<FriendRequest>> GetPendingBetweenAsync(string userId1, string userId2);

        // Recibidas en espera cuyo emisor tiene la cuenta activa, de la más reciente a la más antigua
        Task<List<FriendRequest>> GetPendingReceivedFromActiveAsync(string receptorId);

        // Enviadas visibles (no canceladas ni ocultas) cuyo receptor está activo, de la más reciente a la más antigua
        Task<List<FriendRequest>> GetSentVisibleToActiveAsync(string emisorId);

        Task<int> CountPendingReceivedFromActiveAsync(string receptorId);

        // Ids de los usuarios con los que hay una solicitud en espera, en cualquier dirección
        Task<List<string>> GetPendingCounterpartIdsAsync(string userId);
    }
}
