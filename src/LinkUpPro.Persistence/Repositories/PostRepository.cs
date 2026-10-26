using System.Linq.Expressions;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;
using LinkUpPro.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        // Los contadores se calculan en la misma consulta: no se cargan todas las reacciones en memoria
        private static readonly Expression<Func<Post, PostDto>> AResumen = p => new PostDto
        {
            Id = p.Id,
            AutorId = p.AutorId,
            Contenido = p.Contenido,
            TipoContenido = p.TipoContenido,
            RutaImagen = p.RutaImagen,
            UrlYouTube = p.UrlYouTube,
            Privacidad = p.Privacidad,
            PermitirComentarios = p.PermitirComentarios,
            FechaCreacion = p.FechaCreacion,
            FechaModificacion = p.FechaModificacion,
            IsDeleted = p.IsDeleted,
            MeGusta = p.Reactions.Count(r => r.Tipo == TipoReaccion.MeGusta),
            NoMeGusta = p.Reactions.Count(r => r.Tipo == TipoReaccion.NoMeGusta),
            Comentarios = p.Comments.Count(c => !c.IsDeleted)
        };

        public PostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<PostDto>> SearchByAuthorAsync(PostSearchCriteria criteria)
        {
            var consulta = _dbSet.AsNoTracking()
                .Where(p => p.AutorId == criteria.AutorId && !p.IsDeleted);

            return await AplicarFiltros(consulta, criteria)
                .OrderByDescending(p => p.FechaCreacion)
                .ThenByDescending(p => p.Id)
                .Select(AResumen)
                .ToListAsync();
        }

        public async Task<List<PostDto>> SearchFriendsPostsAsync(ICollection<string> autorIds, PostSearchCriteria criteria)
        {
            if (autorIds.Count == 0)
                return new List<PostDto>();

            // Enunciado (Amigos): solo publicaciones activas, no eliminadas y con privacidad "Solo amigos"
            var ids = autorIds.ToList();
            var consulta = _dbSet.AsNoTracking()
                .Where(p => ids.Contains(p.AutorId) && !p.IsDeleted && p.Privacidad == Privacidad.SoloAmigos);

            return await AplicarFiltros(consulta, criteria)
                .OrderByDescending(p => p.FechaCreacion)
                .ThenByDescending(p => p.Id)
                .Select(AResumen)
                .ToListAsync();
        }

        // Filtros comunes del Home y de Amigos: texto, tipo de contenido, fechas y estado de edición
        private static IQueryable<Post> AplicarFiltros(IQueryable<Post> consulta, PostSearchCriteria criteria)
        {
            if (!string.IsNullOrWhiteSpace(criteria.Texto))
            {
                // Sin distinguir mayúsculas y sin espacios al inicio ni al final
                var texto = criteria.Texto.Trim().ToLower();
                consulta = consulta.Where(p => p.Contenido.ToLower().Contains(texto));
            }

            if (criteria.TipoContenido is TipoContenido tipo)
                consulta = consulta.Where(p => p.TipoContenido == tipo);

            if (criteria.DesdeUtc is DateTime desde)
                consulta = consulta.Where(p => p.FechaCreacion >= desde);

            if (criteria.HastaUtcExclusivo is DateTime hasta)
                consulta = consulta.Where(p => p.FechaCreacion < hasta);

            return criteria.Edicion switch
            {
                FiltroEdicion.Editadas => consulta.Where(p => p.FechaModificacion != null),
                FiltroEdicion.NoEditadas => consulta.Where(p => p.FechaModificacion == null),
                _ => consulta
            };
        }

        public Task<PostDto?> GetItemAsync(int postId) =>
            _dbSet.AsNoTracking()
                .Where(p => p.Id == postId)
                .Select(AResumen)
                .FirstOrDefaultAsync();
    }
}