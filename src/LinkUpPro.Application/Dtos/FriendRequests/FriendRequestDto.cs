using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.FriendRequests
{
    public class FriendRequestDto
    {
        public int Id { get; set; }
        public EstadoSolicitud Estado { get; set; }
        public DateTime FechaEnvio { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        // El otro usuario: el emisor en las recibidas, el receptor en las enviadas
        public UserSummaryDto OtroUsuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
    }
}
