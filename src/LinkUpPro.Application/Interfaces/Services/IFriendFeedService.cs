using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Friends;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Publicaciones de amigos y perfil del amigo (enunciado, pantalla Amigos).
    // Los comentarios y reacciones se reutilizan de IInteractionService, que revalida todo al guardar.
    public interface IFriendFeedService
    {
        // Amigos activos para el select "Amigo" del filtro
        Task<List<UserSummaryDto>> GetFriendsForFilterAsync(string userId);

        Task<ServiceResult<List<PostDto>>> GetFriendsPostsAsync(string userId, FriendPostFilterDto filter);

        Task<ServiceResult<FriendProfileDto>> GetFriendProfileAsync(string userId, string friendId);
    }
}
