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

            consulta = criteria.Edicion switch
            {
                FiltroEdicion.Editadas => consulta.Where(p => p.FechaModificacion != null),
                FiltroEdicion.NoEditadas => consulta.Where(p => p.FechaModificacion == null),
                _ => consulta
            };

            return await consulta
                .OrderByDescending(p => p.FechaCreacion)
                .ThenByDescending(p => p.Id)
                .Select(AResumen)
                .ToListAsync();
        }

        public Task<PostDto?> GetItemAsync(int postId) =>
            _dbSet.AsNoTracking()
                .Where(p => p.Id == postId)
                .Select(AResumen)
                .FirstOrDefaultAsync();
    }
}