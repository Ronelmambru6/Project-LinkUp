using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Friends;
using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Reglas comunes de amistades. Se reutiliza en Solicitudes (#8), Amigos (#20 y #21) y Battleship.
    public interface IFriendshipService
    {
        Task<bool> AreFriendsAsync(string userId1, string userId2);
        Task<int> CountMutualFriendsAsync(string userId, string otherUserId);
        Task<List<UserSummaryDto>> GetMutualFriendsAsync(string userId, string otherUserId);

        // Pantalla Amigos (#20)
        Task<FriendsSummaryDto> GetSummaryAsync(string userId);
        Task<List<FriendDto>> GetFriendsAsync(string userId, string? search);

        // Valida la amistad antes de mostrar la confirmación de eliminar
        Task<ServiceResult<UserSummaryDto>> GetFriendForRemovalAsync(string userId, string friendId);
        Task<ServiceResult> RemoveFriendAsync(string userId, string friendId);
    }
}
