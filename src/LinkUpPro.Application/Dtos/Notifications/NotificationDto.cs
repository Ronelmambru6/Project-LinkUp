using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public UserSummaryDto Actor { get; set; } = new();
        public TipoNotificacion Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
    }

    // A dónde lleva una notificación cuando el contenido sigue disponible
    public class NotificationTargetDto
    {
        public int PostId { get; set; }
        public int? CommentId { get; set; }
    }
}
