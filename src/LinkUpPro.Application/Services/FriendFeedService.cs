using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Friends;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;

namespace LinkUpPro.Application.Services
{
    public class FriendFeedService : IFriendFeedService
    {
        private const string FechasInvertidas = "La fecha inicial no puede ser posterior a la fecha final.";
        private const string AmigoNoDisponible = "El usuario seleccionado ya no forma parte de su lista de amigos.";
        private const string SinPermisoPerfil = "No posee permisos para visualizar el perfil de este usuario.";

        private readonly IPostRepository _postRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly IUserQueryRepository _userQueryRepository;

        public FriendFeedService(
            IPostRepository postRepository,
            IFriendshipRepository friendshipRepository,
            IFriendshipService friendshipService,
            IUserQueryRepository userQueryRepository)
        {
            _postRepository = postRepository;
            _friendshipRepository = friendshipRepository;
            _friendshipService = friendshipService;
            _userQueryRepository = userQueryRepository;
        }

        public async Task<List<UserSummaryDto>> GetFriendsForFilterAsync(string userId) =>
            await _userQueryRepository.GetActiveByIdsAsync(await _friendshipRepository.GetActiveFriendIdsAsync(userId));

        public async Task<ServiceResult<List<PostDto>>> GetFriendsPostsAsync(string userId, FriendPostFilterDto filter)
        {
            if (filter.FechaDesde is DateTime desde && filter.FechaHasta is DateTime hasta && desde.Date > hasta.Date)
                return ServiceResult<List<PostDto>>.Failure(FechasInvertidas);

            // Amistades activas con cuentas activas. Los autores siempre salen de aquí, nunca del formulario:
            // así ningún filtro permite ver publicaciones de quien no es amigo.
            var amigos = await _friendshipRepository.GetActiveFriendIdsAsync(userId);

            List<string> autores;
            if (!string.IsNullOrWhiteSpace(filter.AmigoId))
            {
                if (!amigos.Contains(filter.AmigoId))
                    return ServiceResult<List<PostDto>>.Failure(AmigoNoDisponible);

                autores = new List<string> { filter.AmigoId };
            }
            else
            {
                autores = amigos;
            }

            var publicaciones = await _postRepository.SearchFriendsPostsAsync(autores, CrearCriterios(filter));
            await CompletarAutoresAsync(publicaciones);

            return ServiceResult<List<PostDto>>.Success(publicaciones);
        }

        public async Task<ServiceResult<FriendProfileDto>> GetFriendProfileAsync(string userId, string friendId)
        {
            // Enunciado: ambos existen, ambas cuentas activas y amistad activa
            if (string.IsNullOrWhiteSpace(friendId) || friendId == userId
                || !await _userQueryRepository.IsActiveAsync(userId)
                || !await _friendshipService.AreFriendsAsync(userId, friendId))
                return ServiceResult<FriendProfileDto>.Failure(SinPermisoPerfil);

            var amigo = await _userQueryRepository.GetActiveByIdAsync(friendId);
            if (amigo is null)
                return ServiceResult<FriendProfileDto>.Failure(SinPermisoPerfil);

            var publicaciones = await _postRepository.SearchFriendsPostsAsync(
                new List<string> { friendId }, new PostSearchCriteria());
            foreach (var publicacion in publicaciones)
                publicacion.Autor = amigo;

            return ServiceResult<FriendProfileDto>.Success(new FriendProfileDto
            {
                Usuario = amigo,
                AmigosEnComun = await _friendshipService.CountMutualFriendsAsync(userId, friendId),
                Publicaciones = publicaciones
            });
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        // Mismo criterio de fechas que el Home: el usuario filtra por días de su hora local
        // y "Fecha hasta" incluye el día completo (límite exclusivo al inicio del día siguiente).
        private static PostSearchCriteria CrearCriterios(FriendPostFilterDto filter) => new()
        {
            Texto = filter.Texto?.Trim(),
            TipoContenido = filter.TipoContenido,
            DesdeUtc = filter.FechaDesde is DateTime d ? InicioDelDiaEnUtc(d) : null,
            HastaUtcExclusivo = filter.FechaHasta is DateTime h ? InicioDelDiaEnUtc(h.Date.AddDays(1)) : null,
            Edicion = filter.Edicion
        };

        private async Task CompletarAutoresAsync(List<PostDto> publicaciones)
        {
            var autores = (await _userQueryRepository.GetActiveByIdsAsync(publicaciones.Select(p => p.AutorId).Distinct()))
                .ToDictionary(u => u.Id);

            foreach (var publicacion in publicaciones)
            {
                if (autores.TryGetValue(publicacion.AutorId, out var autor))
                    publicacion.Autor = autor;
            }
        }

        private static DateTime InicioDelDiaEnUtc(DateTime fechaLocal) =>
            new DateTime(fechaLocal.Year, fechaLocal.Month, fechaLocal.Day, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
    }
}
