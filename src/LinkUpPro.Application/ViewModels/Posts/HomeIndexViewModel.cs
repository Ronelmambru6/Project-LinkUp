namespace LinkUpPro.Application.ViewModels.Posts
{
    public class HomeIndexViewModel
    {
        public SavePostViewModel Nueva { get; set; } = new();
        public PostFilterViewModel Filtros { get; set; } = new();
        public List<PostViewModel> Publicaciones { get; set; } = new();

        // true si la lista que se muestra salió de aplicar filtros (cambia el mensaje de lista vacía)
        public bool BusquedaConFiltros { get; set; }
    }
}