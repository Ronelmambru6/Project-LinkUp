using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserQueryRepository _userQueryRepository;

        public FriendshipService(IFriendshipRepository friendshipRepository, IUserQueryRepository userQueryRepository)
        {
            _friendshipRepository = friendshipRepository;
            _userQueryRepository = userQueryRepository;
        }

        public async Task<bool> AreFriendsAsync(string userId1, string userId2)
        {
            var (usuarioA, usuarioB) = UserPair.Order(userId1, userId2);
            var amistad = await _friendshipRepository.GetByPairAsync(usuarioA, usuarioB);
            return amistad?.Estado == EstadoAmistad.Activa;
        }

        public async Task<int> CountMutualFriendsAsync(string userId, string otherUserId) =>
            (await GetMutualFriendIdsAsync(userId, otherUserId)).Count;

        public async Task<List<UserSummaryDto>> GetMutualFriendsAsync(string userId, string otherUserId) =>
            await _userQueryRepository.GetActiveByIdsAsync(await GetMutualFriendIdsAsync(userId, otherUserId));

        // Enunciado: amigo en común = usuario activo, con amistad activa con ambos, que no sea ninguno de los dos
        private async Task<List<string>> GetMutualFriendIdsAsync(string userId, string otherUserId)
        {
            var amigosUsuario = await _friendshipRepository.GetActiveFriendIdsAsync(userId);
            var amigosOtro = await _friendshipRepository.GetActiveFriendIdsAsync(otherUserId);

            return amigosUsuario
                .Intersect(amigosOtro)
                .Where(id => id != userId && id != otherUserId)
                .ToList();
        }
    }
}
