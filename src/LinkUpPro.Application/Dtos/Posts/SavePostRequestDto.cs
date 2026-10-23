using LinkUpPro.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Application.Dtos.Posts
{
    // Solo los campos que el usuario puede enviar. El autor, la fecha y el estado
    // nunca vienen del formulario.
    public class SavePostRequestDto
    {
        public string Contenido { get; set; } = string.Empty;
        public TipoContenido? TipoContenido { get; set; }
        public IFormFile? Imagen { get; set; }
        public string? UrlYouTube { get; set; }
        public Privacidad? Privacidad { get; set; }
        public bool PermitirComentarios { get; set; } = true;
    }
}