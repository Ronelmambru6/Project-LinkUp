using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    // Una sola relación por pareja de usuarios, sin importar quién envió la solicitud.
    public class Friendship : BaseEntity
    {
        public string UsuarioAId { get; set; } = string.Empty;
        public string UsuarioBId { get; set; } = string.Empty;
        public EstadoAmistad Estado { get; set; } = EstadoAmistad.Activa;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaEliminacion { get; set; }
    }
}