using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Friends;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private const string AmistadNoDisponible = "La amistad seleccionada ya no se encuentra disponible.";

        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IUserQueryRepository userQueryRepository,
            IUnitOfWork unitOfWork)
        {
            _friendshipRepository = friendshipRepository;
            _userQueryRepository = userQueryRepository;
            _unitOfWork = unitOfWork;
        }

        // ───────────────────────────── Consultas comunes ─────────────────────────────

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

        // ───────────────────────────── Pantalla Amigos ─────────────────────────────

        public async Task<FriendsSummaryDto> GetSummaryAsync(string userId) => new()
        {
            // Amistades activas cuyos usuarios están activos
            TotalAmigos = (await _friendshipRepository.GetActiveFriendIdsAsync(userId)).Count,
            PublicacionesDisponibles = await _friendshipRepository.CountAvailableFriendPostsAsync(userId)
        };

        public async Task<List<FriendDto>> GetFriendsAsync(string userId, string? search)
        {
            // Solo amistades activas con cuentas activas. GetActiveByIdsAsync ya los ordena
            // alfabéticamente por nombre y apellido, como pide el enunciado.
            var amigoIds = await _friendshipRepository.GetActiveFriendIdsAsync(userId);
            var amigos = await _userQueryRepository.GetActiveByIdsAsync(amigoIds);

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Sin distinguir mayúsculas, sin espacios extremos y con coincidencias parciales
                var filtro = search.Trim();
                amigos = amigos.Where(a =>
                        a.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        a.Apellido.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        a.UserName.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                        a.NombreCompleto.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var resultado = new List<FriendDto>();
            foreach (var amigo in amigos)
            {
                resultado.Add(new FriendDto
                {
                    Usuario = amigo,
                    AmigosEnComun = await CountMutualFriendsAsync(userId, amigo.Id)
                });
            }
            return resultado;
        }

        public async Task<ServiceResult<UserSummaryDto>> GetFriendForRemovalAsync(string userId, string friendId)
        {
            var error = await ValidarEliminacionAsync(userId, friendId);
            if (error is not null)
                return ServiceResult<UserSummaryDto>.Failure(error);

            var amigo = await _userQueryRepository.GetActiveByIdAsync(friendId);
            return amigo is null
                ? ServiceResult<UserSummaryDto>.Failure(AmistadNoDisponible)
                : ServiceResult<UserSummaryDto>.Success(amigo);
        }

        public async Task<ServiceResult> RemoveFriendAsync(string userId, string friendId)
        {
            var resultado = ServiceResult.Success();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Se valida de nuevo justo antes de guardar: la amistad pudo cambiar desde que se abrió la confirmación
                var error = await ValidarEliminacionAsync(userId, friendId);
                if (error is not null)
                {
                    resultado = ServiceResult.Failure(error);
                    return;
                }

                var (usuarioA, usuarioB) = UserPair.Order(userId, friendId);
                var amistad = await _friendshipRepository.GetByPairAsync(usuarioA, usuarioB);

                // Borrado lógico. NO se tocan publicaciones, comentarios, reacciones, notificaciones,
                // solicitudes ni partidas: las partidas activas continúan hasta terminar.
                amistad!.Estado = EstadoAmistad.Eliminada;
                amistad.FechaEliminacion = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();
            });

            return resultado;
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        // Enunciado: ambos existen y están activos, existe una amistad activa y el usuario forma parte de ella.
        // Como la pareja se busca con el Id de la sesión, nadie puede eliminar una amistad ajena.
        private async Task<string?> ValidarEliminacionAsync(string userId, string friendId)
        {
            if (string.IsNullOrWhiteSpace(friendId) || friendId == userId)
                return AmistadNoDisponible;

            if (!await _userQueryRepository.IsActiveAsync(userId) || !await _userQueryRepository.IsActiveAsync(friendId))
                return AmistadNoDisponible;

            var (usuarioA, usuarioB) = UserPair.Order(userId, friendId);
            var amistad = await _friendshipRepository.GetByPairAsync(usuarioA, usuarioB);

            return amistad?.Estado == EstadoAmistad.Activa ? null : AmistadNoDisponible;
        }

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
