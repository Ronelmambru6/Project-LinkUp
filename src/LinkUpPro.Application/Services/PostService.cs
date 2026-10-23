using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    public class PostService : IPostService
    {
        private const string CarpetaImagenes = "posts";
        private const int MaxContenido = 1000;

        private const string NoDisponible = "Esta publicación ya no se encuentra disponible.";
        private const string SinPermisoVer = "No posee permisos para visualizar esta publicación.";
        private const string SinPermisoEditar = "No posee permisos para editar esta publicación.";
        private const string SinPermisoEliminar = "No posee permisos para eliminar esta publicación.";

        private readonly IPostRepository _postRepository;
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly IImageStorageService _imageStorage;
        private readonly IUnitOfWork _unitOfWork;

        public PostService(
            IPostRepository postRepository,
            IUserQueryRepository userQueryRepository,
            IFriendshipService friendshipService,
            IImageStorageService imageStorage,
            IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _userQueryRepository = userQueryRepository;
            _friendshipService = friendshipService;
            _imageStorage = imageStorage;
            _unitOfWork = unitOfWork;
        }

        // ───────────────────────────── Consultas ─────────────────────────────

        public async Task<ServiceResult<List<PostDto>>> GetMyPostsAsync(string userId, PostFilterDto filter)
        {
            if (filter.FechaDesde is DateTime desde && filter.FechaHasta is DateTime hasta && desde.Date > hasta.Date)
                return ServiceResult<List<PostDto>>.Failure("La fecha inicial no puede ser posterior a la fecha final.");

            // Las fechas se guardan en UTC, pero el usuario filtra por días de su hora local.
            // "Fecha hasta" incluye el día completo: el límite es el inicio del día siguiente.
            var criterios = new PostSearchCriteria
            {
                AutorId = userId, // solo publicaciones del usuario autenticado
                Texto = filter.Texto?.Trim(),
                TipoContenido = filter.TipoContenido,
                DesdeUtc = filter.FechaDesde is DateTime d ? InicioDelDiaEnUtc(d) : null,
                HastaUtcExclusivo = filter.FechaHasta is DateTime h ? InicioDelDiaEnUtc(h.Date.AddDays(1)) : null,
                Edicion = filter.Edicion
            };

            var publicaciones = await _postRepository.SearchByAuthorAsync(criterios);
            await CompletarAutoresAsync(publicaciones);

            return ServiceResult<List<PostDto>>.Success(publicaciones);
        }

        public async Task<ServiceResult<PostDto>> GetForViewAsync(int postId, string viewerId)
        {
            var publicacion = await _postRepository.GetItemAsync(postId);
            if (publicacion is null || publicacion.IsDeleted)
                return ServiceResult<PostDto>.Failure(NoDisponible);

            if (!await PuedeVerAsync(publicacion, viewerId))
                return ServiceResult<PostDto>.Failure(SinPermisoVer);

            await CompletarAutoresAsync(new List<PostDto> { publicacion });
            return ServiceResult<PostDto>.Success(publicacion);
        }

        public Task<ServiceResult<PostDto>> GetForEditAsync(int postId, string userId) =>
            GetOwnedAsync(postId, userId, SinPermisoEditar);

        public Task<ServiceResult<PostDto>> GetForDeleteAsync(int postId, string userId) =>
            GetOwnedAsync(postId, userId, SinPermisoEliminar);

        // ───────────────────────────── Crear ─────────────────────────────

        public async Task<ServiceResult> CreateAsync(string userId, SavePostRequestDto request)
        {
            var error = ValidarFormulario(request, esCreacion: true, tieneImagenGuardada: false);
            if (error is not null)
                return error;

            if (!await _userQueryRepository.IsActiveAsync(userId))
                return ServiceResult.Failure("Su cuenta no se encuentra activa.");

            var publicacion = new Post
            {
                AutorId = userId,
                Contenido = request.Contenido.Trim(),
                TipoContenido = request.TipoContenido!.Value,
                Privacidad = request.Privacidad!.Value,
                PermitirComentarios = request.PermitirComentarios,
                FechaCreacion = DateTime.UtcNow
            };

            string? rutaImagen = null;
            if (publicacion.TipoContenido == TipoContenido.Imagen)
            {
                rutaImagen = await _imageStorage.SaveAsync(request.Imagen!, CarpetaImagenes);
                publicacion.RutaImagen = rutaImagen;
            }
            else
            {
                publicacion.UrlYouTube = request.UrlYouTube!.Trim();
            }

            try
            {
                await _postRepository.AddAsync(publicacion);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Si no se pudo guardar, no se deja la imagen huérfana en disco
                if (rutaImagen is not null)
                    await _imageStorage.DeleteAsync(rutaImagen);
                throw;
            }

            return ServiceResult.Success();
        }

        // ───────────────────────────── Editar ─────────────────────────────

        public async Task<ServiceResult> UpdateAsync(int postId, string userId, SavePostRequestDto request)
        {
            var publicacion = await _postRepository.GetByIdAsync(postId);
            if (publicacion is null || publicacion.IsDeleted)
                return ServiceResult.Failure(NoDisponible);

            // Solo el autor: el Id sale de la sesión, nunca del formulario
            if (publicacion.AutorId != userId)
                return ServiceResult.Failure(SinPermisoEditar);

            var tieneImagenGuardada =
                publicacion.TipoContenido == TipoContenido.Imagen && !string.IsNullOrEmpty(publicacion.RutaImagen);

            var error = ValidarFormulario(request, esCreacion: false, tieneImagenGuardada);
            if (error is not null)
                return error;

            var rutaAnterior = publicacion.RutaImagen;
            string? rutaNueva = null;

            publicacion.Contenido = request.Contenido.Trim();
            publicacion.TipoContenido = request.TipoContenido!.Value;
            publicacion.Privacidad = request.Privacidad!.Value;
            publicacion.PermitirComentarios = request.PermitirComentarios;
            publicacion.FechaModificacion = DateTime.UtcNow;
            // No se tocan: AutorId, FechaCreacion, comentarios, respuestas ni reacciones

            if (publicacion.TipoContenido == TipoContenido.Imagen)
            {
                publicacion.UrlYouTube = null; // el enlace anterior se desvincula

                if (request.Imagen is { Length: > 0 })
                {
                    rutaNueva = await _imageStorage.SaveAsync(request.Imagen, CarpetaImagenes);
                    publicacion.RutaImagen = rutaNueva;
                }
            }
            else
            {
                publicacion.UrlYouTube = request.UrlYouTube!.Trim();
                publicacion.RutaImagen = null; // la imagen anterior se desvincula
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                if (rutaNueva is not null)
                    await _imageStorage.DeleteAsync(rutaNueva);
                throw;
            }

            // Ya guardado: se borra del disco la imagen que dejó de estar asociada
            if (!string.IsNullOrEmpty(rutaAnterior) && rutaAnterior != publicacion.RutaImagen)
                await _imageStorage.DeleteAsync(rutaAnterior);

            return ServiceResult.Success();
        }

        // ───────────────────────────── Eliminar ─────────────────────────────

        public async Task<ServiceResult> DeleteAsync(int postId, string userId)
        {
            var publicacion = await _postRepository.GetByIdAsync(postId);
            if (publicacion is null || publicacion.IsDeleted)
                return ServiceResult.Failure(NoDisponible);

            if (publicacion.AutorId != userId)
                return ServiceResult.Failure(SinPermisoEliminar);

            // Borrado lógico: las reacciones, comentarios y notificaciones se conservan
            publicacion.IsDeleted = true;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        // ───────────────────────────── Validaciones ─────────────────────────────

        // Devuelve el primer error del formulario o null si todo es válido.
        // La imagen que ya tiene la publicación cuenta como válida al editar.
        private ServiceResult? ValidarFormulario(SavePostRequestDto request, bool esCreacion, bool tieneImagenGuardada)
        {
            var contenido = request.Contenido?.Trim() ?? string.Empty;

            if (contenido.Length == 0)
                return ServiceResult.Failure("Contenido", "Debe ingresar el contenido de la publicación.");

            if (contenido.Length > MaxContenido)
                return ServiceResult.Failure("Contenido", "El contenido no puede superar los 1000 caracteres.");

            if (request.TipoContenido is null || !Enum.IsDefined(request.TipoContenido.Value))
                return ServiceResult.Failure("TipoContenido", "Debe seleccionar el tipo de contenido.");

            if (request.Privacidad is null || !Enum.IsDefined(request.Privacidad.Value))
                return ServiceResult.Failure("Privacidad", "Debe seleccionar la privacidad.");

            var hayImagen = request.Imagen is { Length: > 0 };
            var hayEnlace = !string.IsNullOrWhiteSpace(request.UrlYouTube);

            // Nunca se permiten los dos a la vez, ni siquiera si alguien manipula el formulario
            if (hayImagen && hayEnlace)
                return ServiceResult.Failure("No puede enviar una imagen y un enlace de YouTube al mismo tiempo.");

            if (request.TipoContenido == TipoContenido.Imagen)
            {
                if (hayEnlace)
                    return ServiceResult.Failure("UrlYouTube", "Con el tipo Imagen no puede ingresar un enlace de YouTube.");

                if (!hayImagen)
                {
                    if (tieneImagenGuardada)
                        return null; // al editar se conserva la imagen actual

                    return ServiceResult.Failure("Imagen", esCreacion
                        ? "Debe seleccionar una imagen para crear la publicación."
                        : "Debe seleccionar una imagen para la publicación.");
                }

                if (!_imageStorage.IsValidImage(request.Imagen!))
                    return ServiceResult.Failure("Imagen", "El archivo seleccionado no tiene un formato de imagen válido.");
            }
            else
            {
                if (hayImagen)
                    return ServiceResult.Failure("Imagen", "Con el tipo Video de YouTube no puede adjuntar una imagen.");

                if (!YouTubeLinkParser.TryGetVideoId(request.UrlYouTube, out _))
                    return ServiceResult.Failure("UrlYouTube", "Debe ingresar un enlace válido de YouTube.");
            }

            return null;
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        private async Task<ServiceResult<PostDto>> GetOwnedAsync(int postId, string userId, string mensajeSinPermiso)
        {
            var publicacion = await _postRepository.GetItemAsync(postId);
            if (publicacion is null || publicacion.IsDeleted)
                return ServiceResult<PostDto>.Failure(NoDisponible);

            if (publicacion.AutorId != userId)
                return ServiceResult<PostDto>.Failure(mensajeSinPermiso);

            await CompletarAutoresAsync(new List<PostDto> { publicacion });
            return ServiceResult<PostDto>.Success(publicacion);
        }

        // Solo yo: únicamente el autor. Solo amigos: el autor y sus amigos actuales.
        private async Task<bool> PuedeVerAsync(PostDto publicacion, string viewerId)
        {
            if (publicacion.AutorId == viewerId)
                return true;

            if (publicacion.Privacidad != Privacidad.SoloAmigos)
                return false;

            return await _friendshipService.AreFriendsAsync(publicacion.AutorId, viewerId);
        }

        private async Task CompletarAutoresAsync(List<PostDto> publicaciones)
        {
            var autores = (await _userQueryRepository.GetActiveByIdsAsync(publicaciones.Select(p => p.AutorId)))
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