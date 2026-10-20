using LinkUpPro.Application.ViewModels.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    public class FriendRequestViewModel
    {
        public int Id { get; set; }
        public EstadoSolicitud Estado { get; set; }
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaRespuesta { get; set; }
        public UserSummaryViewModel OtroUsuario { get; set; } = new();
        public int AmigosEnComun { get; set; }

        public string EstadoTexto => Estado switch
        {
            EstadoSolicitud.EnEspera => "En espera de respuesta",
            EstadoSolicitud.Aceptada => "Aceptada",
            EstadoSolicitud.Rechazada => "Rechazada",
            _ => "Cancelada"
        };

        public bool PuedeCancelar => Estado == EstadoSolicitud.EnEspera;
        public bool PuedeEliminarDelHistorial => Estado is EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada;
    }
}
