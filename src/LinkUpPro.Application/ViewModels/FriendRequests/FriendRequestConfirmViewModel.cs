using LinkUpPro.Application.Dtos.FriendRequests;

namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    // Pantallas de confirmación: aceptar, rechazar, cancelar y eliminar del historial
    public class FriendRequestConfirmViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public FriendRequestAction Accion { get; set; }
    }
}
