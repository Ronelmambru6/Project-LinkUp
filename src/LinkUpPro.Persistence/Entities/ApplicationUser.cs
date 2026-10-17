using Microsoft.AspNetCore.Identity;

namespace LinkUpPro.Persistence.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string FotoPerfil { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? FechaModificacion { get; set; }
    }
}