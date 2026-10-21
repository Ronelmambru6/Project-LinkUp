using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Reglas comunes de amistades. Se reutiliza en Solicitudes (#8) y en Amigos (#20 y #21).
    public interface IFriendshipService
    {
        Task<bool> AreFriendsAsync(string userId1, string userId2);
        Task<int> CountMutualFriendsAsync(string userId, string otherUserId);
        Task<List<UserSummaryDto>> GetMutualFriendsAsync(string userId, string otherUserId);
    }
}
