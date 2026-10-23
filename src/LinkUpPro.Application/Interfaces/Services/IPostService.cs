using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Posts;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Reglas de las publicaciones del propio usuario (enunciado pp. 14–27).
    // El Id del usuario siempre llega de la sesión; la privacidad y los permisos se validan aquí.
    public interface IPostService
    {
        // Home: publicaciones propias (ambas privacidades) con búsqueda y filtros
        Task<ServiceResult<List<PostDto>>> GetMyPostsAsync(string userId, PostFilterDto filter);

        // Acceso directo por URL: valida existencia y privacidad
        Task<ServiceResult<PostDto>> GetForViewAsync(int postId, string viewerId);

        // Pantallas de editar y de confirmar eliminación: solo el autor
        Task<ServiceResult<PostDto>> GetForEditAsync(int postId, string userId);
        Task<ServiceResult<PostDto>> GetForDeleteAsync(int postId, string userId);

        Task<ServiceResult> CreateAsync(string userId, SavePostRequestDto request);
        Task<ServiceResult> UpdateAsync(int postId, string userId, SavePostRequestDto request);
        Task<ServiceResult> DeleteAsync(int postId, string userId);
    }
}