using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Notifications;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Entities;

namespace LinkUpPro.Application.Services
{
    public class NotificationService : INotificationService
    {
        private const string NoEncontrada = "La notificación no existe o no le pertenece.";

        private readonly INotificationRepository _notifications;
        private readonly IGenericRepository<Comment> _comments;
        private readonly IUserQueryRepository _users;
        private readonly IPostService _postService;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(
            INotificationRepository notifications,
            IGenericRepository<Comment> comments,
            IUserQueryRepository users,
            IPostService postService,
            IUnitOfWork unitOfWork)
        {
            _notifications = notifications;
            _comments = comments;
            _users = users;
            _postService = postService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<NotificationDto>> GetMineAsync(string userId)
        {
            var notificaciones = await _notifications.GetByRecipientAsync(userId);
            if (notificaciones.Count == 0)
                return new List<NotificationDto>();

            var actores = (await _users.GetActiveByIdsAsync(notificaciones.Select(n => n.ActorId).Distinct().ToList()))
                .ToDictionary(u => u.Id);

            return notificaciones.Select(n => new NotificationDto
            {
                Id = n.Id,
                Actor = actores.TryGetValue(n.ActorId, out var actor)
                    ? actor
                    : new UserSummaryDto { Id = n.ActorId, UserName = "Usuario no disponible" },
                Tipo = n.Tipo,
                Descripcion = n.Descripcion,
                Fecha = n.Fecha,
                Leida = n.Leida
            }).ToList();
        }

        public Task<int> CountUnreadAsync(string userId) => _notifications.CountUnreadAsync(userId);

        public async Task<ServiceResult> MarkAsReadAsync(int notificationId, string userId)
        {
            var notificacion = await _notifications.GetByIdAsync(notificationId);

            // Misma respuesta si no existe o es de otro usuario: no se revela qué notificaciones existen
            if (notificacion is null || notificacion.DestinatarioId != userId)
                return ServiceResult.Failure(NoEncontrada);

            if (!notificacion.Leida)
            {
                notificacion.Leida = true;
                await _unitOfWork.SaveChangesAsync();
            }

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> MarkAllAsReadAsync(string userId)
        {
            var pendientes = await _notifications.GetUnreadAsync(userId);
            if (pendientes.Count == 0)
                return ServiceResult.Success();

            foreach (var notificacion in pendientes)
                notificacion.Leida = true;

            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<NotificationTargetDto>> ResolveTargetAsync(int notificationId, string userId)
        {
            var notificacion = await _notifications.GetByIdAsync(notificationId);
            if (notificacion is null || notificacion.DestinatarioId != userId)
                return ServiceResult<NotificationTargetDto>.Failure(NoEncontrada);

            if (notificacion.PostId is not int postId)
                return ServiceResult<NotificationTargetDto>.Failure("El contenido de esta notificación ya no se encuentra disponible.");

            // El servicio de publicaciones ya devuelve "ya no se encuentra disponible" o "No posee permisos…"
            var publicacion = await _postService.GetForViewAsync(postId, userId);
            if (!publicacion.Succeeded)
                return ServiceResult<NotificationTargetDto>.Failure(publicacion.FirstError);

            if (notificacion.CommentId is int commentId)
            {
                var comentario = await _comments.GetByIdAsync(commentId);
                if (comentario is null || comentario.IsDeleted)
                    return ServiceResult<NotificationTargetDto>.Failure("Este comentario ya no se encuentra disponible.");
            }

            return ServiceResult<NotificationTargetDto>.Success(new NotificationTargetDto
            {
                PostId = postId,
                CommentId = notificacion.CommentId
            });
        }
    }
}
