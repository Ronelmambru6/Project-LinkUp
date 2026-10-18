using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    public class FriendRequest : BaseEntity
    {
        public string EmisorId { get; set; } = string.Empty;
        public string ReceptorId { get; set; } = string.Empty;
        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.EnEspera;
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRespuesta { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public bool OcultaParaEmisor { get; set; }
    }
}