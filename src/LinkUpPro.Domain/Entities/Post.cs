using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    public class Post : BaseEntity
    {
        public string AutorId { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty; // máx. 1000 caracteres
        public TipoContenido TipoContenido { get; set; }
        public string? RutaImagen { get; set; }
        public string? UrlYouTube { get; set; }
        public Privacidad Privacidad { get; set; } = Privacidad.SoloAmigos;
        public bool PermitirComentarios { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}