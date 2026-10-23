using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.Comments
{
    // Comentario o respuesta listo para mostrarse. Las banderas las calcula el controlador
    // con el usuario de la sesión; la vista solo decide qué botones dibujar.
    public class CommentViewModel
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public UserSummaryViewModel Autor { get; set; } = new();
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public bool Eliminado { get; set; }
        public bool EsAutor { get; set; }
        public bool PuedeVerPerfil { get; set; }
        public bool PuedeResponder { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeEliminar { get; set; }

        // 0 = comentario; 1, 2… = respuesta (sirve para la indentación)
        public int Nivel { get; set; }

        public List<CommentViewModel> Respuestas { get; set; } = new();

        public bool Editado => FechaModificacion.HasValue;
    }

    // Formulario de editar un comentario. El Id y la publicación salen de la ruta y de la base de datos.
    public class EditCommentViewModel
    {
        public int Id { get; set; }
        public int PostId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el contenido del comentario.")]
        [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
        [Display(Name = "Comentario")]
        public string Contenido { get; set; } = string.Empty;
    }

    public class DeleteCommentViewModel
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public UserSummaryViewModel Autor { get; set; } = new();
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}
