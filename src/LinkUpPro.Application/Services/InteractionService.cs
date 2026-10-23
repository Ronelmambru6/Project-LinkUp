using LinkUpPro.Application.Dtos.Comments;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    public class InteractionService : IInteractionService
    {
        private const int MaxComentario = 500;

        private const string PostNoDisponible = "Esta publicación ya no se encuentra disponible.";
        private const string SinPermisoVer = "No posee permisos para visualizar esta publicación.";
        private const string CuentaInactiva = "Su cuenta no se encuentra activa.";
        private const string ComentariosDesactivados = "Los comentarios están desactivados para esta publicación.";
        private const string ComentarioNoDisponible = "Este comentario ya no se encuentra disponible.";
        private const string SinPermisoEditar = "No posee permisos para editar este comentario.";
        private const string SinPermisoEliminar = "No posee permisos para eliminar este comentario.";

        private readonly IPostRepository _posts;
        private readonly IGenericRepository<Comment> _comments;
        private readonly IGenericRepository<Reaction> _reactions;
        private readonly IGenericRepository<Notification> _notifications;
        private readonly IUserQueryRepository _users;
        private readonly IFriendshipService _friendships;
        private readonly IUnitOfWork _unitOfWork;

        public InteractionService(
            IPostRepository posts,
            IGenericRepository<Comment> comments,
            IGenericRepository<Reaction> reactions,
            IGenericRepository<Notification> notifications,
            IUserQueryRepository users,
            IFriendshipService friendships,
            IUnitOfWork unitOfWork)
        {
            _posts = posts;
            _comments = comments;
            _reactions = reactions;
            _notifications = notifications;
            _users = users;
            _friendships = friendships;
            _unitOfWork = unitOfWork;
        }

        // ───────────────────────────── Comentarios: consulta ─────────────────────────────

        public async Task<ServiceResult<List<CommentDto>>> GetCommentsAsync(int postId, string viewerId)
        {
            var (_, error) = await ValidarAccesoAsync(postId, viewerId);
            if (error is not null)
                return ServiceResult<List<CommentDto>>.Failure(error);

            var todos = await _comments.FindAsync(c => c.PostId == postId);

            // Datos de los autores de los comentarios que se van a mostrar
            var autorIds = todos.Where(c => !c.IsDeleted).Select(c => c.AutorId).Distinct().ToList();
            var autores = (await _users.GetActiveByIdsAsync(autorIds)).ToDictionary(u => u.Id);

            // Foto y usuario siempre se ven; el enlace al perfil solo si es el propio usuario o un amigo
            var conPerfil = new HashSet<string>();
            foreach (var autorId in autorIds)
            {
                if (autorId == viewerId || await _friendships.AreFriendsAsync(viewerId, autorId))
                    conPerfil.Add(autorId);
            }

            var porPadre = todos.ToLookup(c => c.ParentCommentId);

            List<CommentDto> Construir(int? padreId)
            {
                var resultado = new List<CommentDto>();
                foreach (var comentario in porPadre[padreId].OrderBy(c => c.FechaCreacion).ThenBy(c => c.Id))
                {
                    var respuestas = Construir(comentario.Id);

                    // Eliminado y sin respuestas visibles: desaparece del todo
                    if (comentario.IsDeleted && respuestas.Count == 0)
                        continue;

                    var dto = new CommentDto
                    {
                        Id = comentario.Id,
                        PostId = comentario.PostId,
                        AutorId = comentario.AutorId,
                        ParentCommentId = comentario.ParentCommentId,
                        FechaCreacion = comentario.FechaCreacion,
                        IsDeleted = comentario.IsDeleted,
                        Respuestas = respuestas
                    };

                    if (!comentario.IsDeleted)
                    {
                        dto.Contenido = comentario.Contenido;
                        dto.FechaModificacion = comentario.FechaModificacion;
                        dto.PuedeVerPerfil = conPerfil.Contains(comentario.AutorId);
                        dto.Autor = autores.TryGetValue(comentario.AutorId, out var autor)
                            ? autor
                            : new UserSummaryDto { Id = comentario.AutorId, UserName = "Usuario no disponible" };
                    }

                    resultado.Add(dto);
                }
                return resultado;
            }

            return ServiceResult<List<CommentDto>>.Success(Construir(null));
        }

        public Task<ServiceResult<CommentDto>> GetCommentForEditAsync(int commentId, string userId) =>
            GetOwnCommentAsync(commentId, userId, SinPermisoEditar, exigirComentariosActivos: true);

        public Task<ServiceResult<CommentDto>> GetCommentForDeleteAsync(int commentId, string userId) =>
            GetOwnCommentAsync(commentId, userId, SinPermisoEliminar, exigirComentariosActivos: false);

        // ───────────────────────────── Comentarios: cambios ─────────────────────────────

        public async Task<ServiceResult> AddCommentAsync(int postId, string userId, SaveCommentRequestDto request)
        {
            var (post, error) = await ValidarAccesoAsync(postId, userId);
            if (error is not null)
                return ServiceResult.Failure(error);

            if (!post!.PermitirComentarios)
                return ServiceResult.Failure(ComentariosDesactivados);

            var errorContenido = ValidarContenido(request.Contenido);
            if (errorContenido is not null)
                return errorContenido;

            Comment? padre = null;
            if (request.ParentCommentId is int padreId)
            {
                padre = await _comments.GetByIdAsync(padreId);
                if (padre is null || padre.PostId != postId || padre.IsDeleted)
                    return ServiceResult.Failure("El comentario al que intenta responder ya no se encuentra disponible.");
            }

            var comentario = new Comment
            {
                PostId = postId,
                AutorId = userId,
                ParentCommentId = request.ParentCommentId,
                Contenido = request.Contenido.Trim(),
                FechaCreacion = DateTime.UtcNow
            };
            await _comments.AddAsync(comentario);
            await _unitOfWork.SaveChangesAsync();

            await NotificarComentarioAsync(post!, padre, comentario);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> UpdateCommentAsync(int commentId, string userId, string contenido)
        {
            var comentario = await _comments.GetByIdAsync(commentId);
            if (comentario is null || comentario.IsDeleted)
                return ServiceResult.Failure(ComentarioNoDisponible);

            if (comentario.AutorId != userId)
                return ServiceResult.Failure(SinPermisoEditar);

            var (post, error) = await ValidarAccesoAsync(comentario.PostId, userId);
            if (error is not null)
                return ServiceResult.Failure(error);

            if (!post!.PermitirComentarios)
                return ServiceResult.Failure(ComentariosDesactivados);

            var errorContenido = ValidarContenido(contenido);
            if (errorContenido is not null)
                return errorContenido;

            comentario.Contenido = contenido.Trim();
            comentario.FechaModificacion = DateTime.UtcNow;
            // No se tocan: autor, publicación, comentario padre, fecha de creación ni respuestas
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteCommentAsync(int commentId, string userId)
        {
            var comentario = await _comments.GetByIdAsync(commentId);
            if (comentario is null || comentario.IsDeleted)
                return ServiceResult.Failure(ComentarioNoDisponible);

            if (comentario.AutorId != userId)
                return ServiceResult.Failure(SinPermisoEliminar);

            // Borrado lógico: si tiene respuestas, se mostrará "Este comentario fue eliminado."
            comentario.IsDeleted = true;
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        // ───────────────────────────── Reacciones ─────────────────────────────

        public async Task<ServiceResult<TipoReaccion?>> GetMyReactionAsync(int postId, string userId)
        {
            var (_, error) = await ValidarAccesoAsync(postId, userId);
            if (error is not null)
                return ServiceResult<TipoReaccion?>.Failure(error);

            var existente = (await _reactions.FindAsync(r => r.PostId == postId && r.UsuarioId == userId)).FirstOrDefault();
            return ServiceResult<TipoReaccion?>.Success(existente?.Tipo);
        }

        public async Task<ServiceResult> SetReactionAsync(int postId, string userId, TipoReaccion tipo)
        {
            if (!Enum.IsDefined(tipo))
                return ServiceResult.Failure("La reacción seleccionada no es válida.");

            // Las reacciones siguen funcionando aunque los comentarios estén desactivados
            var (post, error) = await ValidarAccesoAsync(postId, userId);
            if (error is not null)
                return ServiceResult.Failure(error);

            var existente = (await _reactions.FindAsync(r => r.PostId == postId && r.UsuarioId == userId)).FirstOrDefault();

            try
            {
                if (existente is null)
                {
                    await _reactions.AddAsync(new Reaction
                    {
                        PostId = postId,
                        UsuarioId = userId,
                        Tipo = tipo,
                        Fecha = DateTime.UtcNow
                    });
                }
                else if (existente.Tipo != tipo)
                {
                    // Una sola reacción por usuario: cambiarla actualiza la existente
                    existente.Tipo = tipo;
                    existente.Fecha = DateTime.UtcNow;
                }
                else
                {
                    // Misma reacción otra vez: no cambia nada y no se notifica
                    return ServiceResult.Success();
                }

                // Reacción nueva o cambiada: se notifica al autor, en la misma operación que guarda la reacción.
                // (Eliminar una reacción no genera notificación.)
                await AgregarNotificacionDeReaccionAsync(post!, userId, tipo);

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Típico de dos clics casi simultáneos: el índice único (publicación, usuario) rechaza el segundo
                return ServiceResult.Failure("No se pudo registrar la reacción. Actualice la pantalla e intente de nuevo.");
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> RemoveReactionAsync(int postId, string userId)
        {
            var (_, error) = await ValidarAccesoAsync(postId, userId);
            if (error is not null)
                return ServiceResult.Failure(error);

            var existente = (await _reactions.FindAsync(r => r.PostId == postId && r.UsuarioId == userId)).FirstOrDefault();
            if (existente is null)
                return ServiceResult.Failure("No tiene una reacción registrada en esta publicación.");

            _reactions.Remove(existente);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResult.Success();
        }

        // ───────────────────────────── Notificaciones ─────────────────────────────

        // Comentario nuevo -> avisa al autor de la publicación; respuesta -> avisa al autor del comentario respondido.
        // No se notifica a quien actúa sobre su propio contenido. Se guarda aparte porque necesita el Id del comentario,
        // y si falla no se deshace el comentario, que ya quedó publicado.
        private async Task NotificarComentarioAsync(PostDto post, Comment? padre, Comment comentario)
        {
            var destinatarioId = padre?.AutorId ?? post.AutorId;
            if (destinatarioId == comentario.AutorId)
                return;

            try
            {
                var actor = await _users.GetActiveByIdAsync(comentario.AutorId);
                if (actor is null)
                    return;

                await _notifications.AddAsync(new Notification
                {
                    DestinatarioId = destinatarioId,
                    ActorId = comentario.AutorId,
                    Tipo = padre is null ? TipoNotificacion.Comentario : TipoNotificacion.Respuesta,
                    Descripcion = padre is null
                        ? $"{actor.UserName} comentó tu publicación."
                        : $"{actor.UserName} respondió tu comentario.",
                    PostId = post.Id,
                    CommentId = comentario.Id,
                    Leida = false,
                    Fecha = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                // La notificación es secundaria: el comentario ya se publicó correctamente
            }
        }

        // Solo agrega la notificación al contexto: la guarda quien llama junto con la reacción
        private async Task AgregarNotificacionDeReaccionAsync(PostDto post, string actorId, TipoReaccion tipo)
        {
            if (post.AutorId == actorId)
                return;

            var actor = await _users.GetActiveByIdAsync(actorId);
            if (actor is null)
                return;

            var texto = tipo == TipoReaccion.MeGusta ? "Me gusta" : "No me gusta";
            await _notifications.AddAsync(new Notification
            {
                DestinatarioId = post.AutorId,
                ActorId = actorId,
                Tipo = TipoNotificacion.Reaccion,
                Descripcion = $"{actor.UserName} reaccionó con {texto} a tu publicación.",
                PostId = post.Id,
                Leida = false,
                Fecha = DateTime.UtcNow
            });
        }

        // ───────────────────────────── Auxiliares ─────────────────────────────

        // Publicación activa, cuentas activas y privacidad (Solo yo / Solo amigos con amistad vigente).
        // Devuelve la publicación o el mensaje del primer problema encontrado.
        private async Task<(PostDto? Post, string? Error)> ValidarAccesoAsync(int postId, string userId)
        {
            var post = await _posts.GetItemAsync(postId);
            if (post is null || post.IsDeleted)
                return (null, PostNoDisponible);

            if (!await _users.IsActiveAsync(userId))
                return (null, CuentaInactiva);

            if (post.AutorId == userId)
                return (post, null);

            // Si la cuenta del autor ya no está activa, la publicación deja de estar disponible
            if (!await _users.IsActiveAsync(post.AutorId))
                return (null, PostNoDisponible);

            if (post.Privacidad != Privacidad.SoloAmigos || !await _friendships.AreFriendsAsync(post.AutorId, userId))
                return (null, SinPermisoVer);

            return (post, null);
        }

        private static ServiceResult? ValidarContenido(string? contenido)
        {
            var texto = contenido?.Trim() ?? string.Empty;

            if (texto.Length == 0)
                return ServiceResult.Failure("Contenido", "Debe ingresar el contenido del comentario.");

            if (texto.Length > MaxComentario)
                return ServiceResult.Failure("Contenido", "El comentario no puede superar los 500 caracteres.");

            return null;
        }

        private async Task<ServiceResult<CommentDto>> GetOwnCommentAsync(
            int commentId, string userId, string mensajeSinPermiso, bool exigirComentariosActivos)
        {
            var comentario = await _comments.GetByIdAsync(commentId);
            if (comentario is null || comentario.IsDeleted)
                return ServiceResult<CommentDto>.Failure(ComentarioNoDisponible);

            if (comentario.AutorId != userId)
                return ServiceResult<CommentDto>.Failure(mensajeSinPermiso);

            if (exigirComentariosActivos)
            {
                var (post, error) = await ValidarAccesoAsync(comentario.PostId, userId);
                if (error is not null)
                    return ServiceResult<CommentDto>.Failure(error);

                if (!post!.PermitirComentarios)
                    return ServiceResult<CommentDto>.Failure(ComentariosDesactivados);
            }

            var autor = await _users.GetActiveByIdAsync(userId) ?? new UserSummaryDto { Id = userId };

            return ServiceResult<CommentDto>.Success(new CommentDto
            {
                Id = comentario.Id,
                PostId = comentario.PostId,
                AutorId = comentario.AutorId,
                Autor = autor,
                ParentCommentId = comentario.ParentCommentId,
                Contenido = comentario.Contenido,
                FechaCreacion = comentario.FechaCreacion,
                FechaModificacion = comentario.FechaModificacion,
                PuedeVerPerfil = true
            });
        }
    }
}
