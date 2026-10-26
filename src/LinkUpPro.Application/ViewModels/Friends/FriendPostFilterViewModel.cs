using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.ViewModels.Posts;

namespace LinkUpPro.Application.ViewModels.Friends
{
    // Filtros de publicaciones de amigos: los del Home (texto, tipo, fechas y edición) más el amigo.
    // La validación de fechas se hereda; que el amigo siga siéndolo lo valida el servicio.
    public class FriendPostFilterViewModel : PostFilterViewModel
    {
        [Display(Name = "Amigo")]
        public string? AmigoId { get; set; }

        public bool HayFiltrosAmigos => HayFiltros || !string.IsNullOrWhiteSpace(AmigoId);
    }
}
