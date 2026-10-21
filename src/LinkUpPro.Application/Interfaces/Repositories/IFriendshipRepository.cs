using LinkUpPro.Domain.Entities;

namespace LinkUpPro.Application.Interfaces.Repositories
{
    public interface IFriendshipRepository : IGenericRepository<Friendship>
    {
        // Espera la pareja ya ordenada con UserPair.Order
        Task<Friendship?> GetByPairAsync(string usuarioAId, string usuarioBId);

        // Ids de los amigos con amistad activa y cuenta activa
        Task<List<string>> GetActiveFriendIdsAsync(string userId);
    }
}
