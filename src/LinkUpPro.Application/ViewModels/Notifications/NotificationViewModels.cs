using LinkUpPro.Application.ViewModels.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.Notifications
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public UserSummaryViewModel Actor { get; set; } = new();
        public TipoNotificacion Tipo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }

        public string TipoTexto => Tipo switch
        {
            TipoNotificacion.Comentario => "Comentario",
            TipoNotificacion.Respuesta => "Respuesta",
            _ => "Reacción"
        };

        public string EstadoTexto => Leida ? "Leída" : "No leída";
    }

    public class NotificationsIndexViewModel
    {
        public List<NotificationViewModel> Notificaciones { get; set; } = new();
        public int NoLeidas => Notificaciones.Count(n => !n.Leida);
    }
}
