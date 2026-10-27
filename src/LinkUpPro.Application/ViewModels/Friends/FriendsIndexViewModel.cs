using LinkUpPro.Application.ViewModels.Posts;
using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.Friends
{
    public class FriendsIndexViewModel
    {
        // Resumen de amistades
        public int TotalAmigos { get; set; }
        public int PublicacionesDisponibles { get; set; }

        // Publicaciones de amigos con sus filtros
        public FriendPostFilterViewModel Filtros { get; set; } = new();
        public List<UserSummaryViewModel> AmigosFiltro { get; set; } = new();
        public List<string> ErroresFiltro { get; set; } = new();
        public List<PostViewModel> Publicaciones { get; set; } = new();

        // true si la lista salió de aplicar filtros (cambia el mensaje de lista vacía)
        public bool BusquedaConFiltros { get; set; }

        // Listado de amigos
        public string? Search { get; set; }
        public List<FriendViewModel> Amigos { get; set; } = new();
    }
}
