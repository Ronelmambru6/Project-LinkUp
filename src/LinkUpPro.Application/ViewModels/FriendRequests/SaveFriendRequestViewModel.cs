using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    // Pantalla "Nueva solicitud de amistad"
    public class SaveFriendRequestViewModel
    {
        // Texto del buscador por nombre de usuario
        public string? Search { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un usuario para enviar la solicitud de amistad.")]
        public string? SelectedUserId { get; set; }

        // Solo para mostrar: no llega en el formulario, el controlador lo vuelve a cargar
        [ValidateNever]
        public List<AvailableUserViewModel> Usuarios { get; set; } = new();
    }
}
