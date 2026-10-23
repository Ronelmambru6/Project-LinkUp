using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Dtos.Comments
{
    // Comentario o respuesta ya armado en árbol (las respuestas cuelgan de Respuestas).
    public class CommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AutorId { get; set; } = string.Empty;
        public UserSummaryDto Autor { get; set; } = new();
        public int? ParentCommentId { get; set; }
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Eliminado con respuestas: se muestra solo "Este comentario fue eliminado."
        public bool IsDeleted { get; set; }

        // El autor solo tiene enlace al perfil si es el propio usuario o es su amigo
        public bool PuedeVerPerfil { get; set; }

        public List<CommentDto> Respuestas { get; set; } = new();
    }
}
