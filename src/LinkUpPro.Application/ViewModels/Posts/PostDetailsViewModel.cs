using LinkUpPro.Application.ViewModels.Comments;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.Posts
{
    // Detalle de una publicación con sus reacciones y comentarios.
    public class PostDetailsViewModel
    {
        public PostViewModel Post { get; set; } = new();

        // Reacción del usuario que mira (null = todavía no reaccionó)
        public TipoReaccion? MiReaccion { get; set; }

        public List<CommentViewModel> Comentarios { get; set; } = new();
    }
}
