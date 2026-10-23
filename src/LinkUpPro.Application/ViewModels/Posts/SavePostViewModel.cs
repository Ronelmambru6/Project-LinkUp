using System.ComponentModel.DataAnnotations;
using LinkUpPro.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LinkUpPro.Application.ViewModels.Posts
{
    // Formulario para crear y editar publicaciones.
    // La validación completa (imagen, enlace, permisos) la repite el servicio: el formulario no es de fiar.
    public class SavePostViewModel
    {
        // Solo lectura: al editar se vuelven a cargar de la base de datos, nunca del formulario
        [BindNever, ValidateNever]
        public int Id { get; set; }

        [BindNever, ValidateNever]
        public string? RutaImagenActual { get; set; }

        [Required(ErrorMessage = "Debe ingresar el contenido de la publicación.")]
        [StringLength(1000, ErrorMessage = "El contenido no puede superar los 1000 caracteres.")]
        [Display(Name = "Contenido")]
        public string Contenido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el tipo de contenido.")]
        [Display(Name = "Tipo de contenido")]
        public TipoContenido? TipoContenido { get; set; }

        [Display(Name = "Imagen")]
        public IFormFile? Imagen { get; set; }

        [Display(Name = "Enlace de YouTube")]
        [StringLength(500, ErrorMessage = "El enlace no puede superar los 500 caracteres.")]
        public string? UrlYouTube { get; set; }

        [Required(ErrorMessage = "Debe seleccionar la privacidad.")]
        [Display(Name = "Privacidad")]
        public Privacidad? Privacidad { get; set; } = Domain.Enums.Privacidad.SoloAmigos;

        [Display(Name = "Permitir comentarios")]
        public bool PermitirComentarios { get; set; } = true;
    }
}