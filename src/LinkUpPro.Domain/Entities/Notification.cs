using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string DestinatarioId { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public TipoNotificacion Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        public bool Leida { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}