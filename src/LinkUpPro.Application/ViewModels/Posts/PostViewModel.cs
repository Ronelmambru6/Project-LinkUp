using LinkUpPro.Application.ViewModels.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.Posts
{
    // Publicación lista para mostrarse (Home, detalle y confirmación de eliminación).
    public class PostViewModel
    {
        public int Id { get; set; }
        public string AutorId { get; set; } = string.Empty;
        public UserSummaryViewModel Autor { get; set; } = new();
        public string Contenido { get; set; } = string.Empty;
        public TipoContenido TipoContenido { get; set; }
        public string? RutaImagen { get; set; }
        public string? UrlYouTube { get; set; }
        public string? YouTubeVideoId { get; set; }
        public Privacidad Privacidad { get; set; }
        public bool PermitirComentarios { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int MeGusta { get; set; }
        public int NoMeGusta { get; set; }
        public int Comentarios { get; set; }

        // El controlador lo calcula comparando con el usuario de la sesión;
        // los botones Editar y Eliminar solo se muestran al autor
        public bool EsAutor { get; set; }

        // El controlador lo activa cuando el autor es un amigo: su nombre y foto enlazan a su perfil
        public bool PuedeVerPerfilAutor { get; set; }

        public bool Editada => FechaModificacion.HasValue;

        public string PrivacidadTexto => Privacidad == Privacidad.SoloAmigos ? "Solo amigos" : "Solo yo";

        public string ComentariosTexto => PermitirComentarios ? "Comentarios permitidos" : "Comentarios desactivados";

        // youtube-nocookie.com: el video se incrusta sin cookies de seguimiento hasta que se reproduce
        public string? YouTubeEmbedUrl =>
            string.IsNullOrEmpty(YouTubeVideoId) ? null : $"https://www.youtube-nocookie.com/embed/{YouTubeVideoId}";
    }
}