using LinkUpPro.Domain.Common;

namespace LinkUpPro.Domain.Entities
{
    public class Comment : BaseEntity
    {
        public int PostId { get; set; }
        public string AutorId { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; } // null = comentario; con valor = respuesta
        public string Contenido { get; set; } = string.Empty; // máx. 500 caracteres
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
        public bool IsDeleted { get; set; }

        public Post? Post { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}