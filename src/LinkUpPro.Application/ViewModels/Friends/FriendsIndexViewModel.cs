namespace LinkUpPro.Application.ViewModels.Friends
{
    public class FriendsIndexViewModel
    {
        // Resumen de amistades
        public int TotalAmigos { get; set; }
        public int PublicacionesDisponibles { get; set; }

        // Listado de amigos
        public string? Search { get; set; }
        public List<FriendViewModel> Amigos { get; set; } = new();
    }
}
