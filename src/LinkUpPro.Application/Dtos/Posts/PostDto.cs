using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Posts
{
    public class PostDto
    {
        public int Id { get; set; }
        public string AutorId { get; set; } = string.Empty;
        public UserSummaryDto Autor { get; set; } = new();
        public string Contenido { get; set; } = string.Empty;
        public TipoContenido TipoContenido { get; set; }
        public string? RutaImagen { get; set; }
        public string? UrlYouTube { get; set; }
        public Privacidad Privacidad { get; set; }
        public bool PermitirComentarios { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public bool IsDeleted { get; set; }

        public int MeGusta { get; set; }
        public int NoMeGusta { get; set; }
        public int Comentarios { get; set; }

        // Id del video para incrustarlo; null si la publicación no tiene un video válido
        public string? YouTubeVideoId =>
            TipoContenido == TipoContenido.YouTube && YouTubeLinkParser.TryGetVideoId(UrlYouTube, out var id) ? id : null;
    }
}