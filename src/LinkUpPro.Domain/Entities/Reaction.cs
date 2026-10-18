using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    // Una reacción por usuario y publicación.
    public class Reaction : BaseEntity
    {
        public int PostId { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public TipoReaccion Tipo { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Post? Post { get; set; }
    }
}