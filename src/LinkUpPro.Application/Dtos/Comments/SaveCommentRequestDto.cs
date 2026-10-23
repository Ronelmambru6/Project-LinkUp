namespace LinkUpPro.Application.Dtos.Comments
{
    // Solo lo que el usuario puede enviar: el autor, la fecha y la publicación no vienen del formulario.
    public class SaveCommentRequestDto
    {
        public string Contenido { get; set; } = string.Empty;

        // null = comentario nuevo; con valor = respuesta a un comentario o a otra respuesta
        public int? ParentCommentId { get; set; }
    }
}
