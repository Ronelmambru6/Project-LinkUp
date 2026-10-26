using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Domain.Entities;

namespace LinkUpPro.Application.Interfaces.Repositories
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        // Publicaciones del autor que no están eliminadas, de la más reciente a la más antigua,
        // con sus contadores de reacciones y comentarios. El autor se completa en el servicio.
        Task<List<PostDto>> SearchByAuthorAsync(PostSearchCriteria criteria);

        // Publicaciones "Solo amigos" no eliminadas de los autores indicados, con los mismos filtros
        // (se ignora criteria.AutorId). El servicio decide qué autores son amigos activos.
        Task<List<PostDto>> SearchFriendsPostsAsync(ICollection<string> autorIds, PostSearchCriteria criteria);

        // Una publicación con sus contadores, incluso si está eliminada (IsDeleted lo indica)
        Task<PostDto?> GetItemAsync(int postId);
    }
}
