using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    public class Game : BaseEntity
    {
        public string CreadorId { get; set; } = string.Empty;
        public string OponenteId { get; set; } = string.Empty;
        public EstadoPartida Estado { get; set; } = EstadoPartida.Posicionando;
        public string? TurnoUsuarioId { get; set; }
        public DateTime? FechaAsignacionTurno { get; set; } // base del vencimiento de 48 horas
        public string? GanadorId { get; set; }
        public MotivoFinPartida? MotivoFin { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
        public DateTime? FechaFin { get; set; }

        // Token de concurrencia: evita que dos ataques simultáneos se pisen.
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<Ship> Ships { get; set; } = new List<Ship>();
        public ICollection<Move> Moves { get; set; } = new List<Move>();
    }
}