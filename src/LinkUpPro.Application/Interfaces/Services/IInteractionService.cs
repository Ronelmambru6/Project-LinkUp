using LinkUpPro.Application.Dtos.Comments;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Comentarios, respuestas y reacciones de una publicación (enunciado pp. 14–27 y 31–46).
    // El Id del usuario siempre llega de la sesión. Antes de guardar cualquier cambio se vuelve a validar
    // que la publicación esté activa, la privacidad, la amistad y que las cuentas estén activas.
    public interface IInteractionService
    {
        // Comentarios en árbol (más antiguo primero). Valida que el usuario pueda ver la publicación.
        Task<ServiceResult<List<CommentDto>>> GetCommentsAsync(int postId, string viewerId);

        Task<ServiceResult<CommentDto>> GetCommentForEditAsync(int commentId, string userId);
        Task<ServiceResult<CommentDto>> GetCommentForDeleteAsync(int commentId, string userId);

        Task<ServiceResult> AddCommentAsync(int postId, string userId, SaveCommentRequestDto request);
        Task<ServiceResult> UpdateCommentAsync(int commentId, string userId, string contenido);
        Task<ServiceResult> DeleteCommentAsync(int commentId, string userId);

        // Reacción del usuario en la publicación (null si no ha reaccionado)
        Task<ServiceResult<TipoReaccion?>> GetMyReactionAsync(int postId, string userId);
        Task<ServiceResult> SetReactionAsync(int postId, string userId, TipoReaccion tipo);
        Task<ServiceResult> RemoveReactionAsync(int postId, string userId);
    }
}
