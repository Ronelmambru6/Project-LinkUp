namespace LinkUpPro.Application.Dtos.Posts
{
    // Filtros de la pantalla Amigos: los mismos del Home más el amigo seleccionado.
    public class FriendPostFilterDto : PostFilterDto
    {
        public string? AmigoId { get; set; }
    }
}
