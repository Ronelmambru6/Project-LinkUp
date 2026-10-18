using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Battleship
{
    // Tablero de ataque del usuario logueado: solo muestra sus propios ataques.
    public class AttackBoardDto
    {
        public int GameId { get; set; }
        public string OponenteId { get; set; } = string.Empty;
        public EstadoPartida Estado { get; set; }
        public string? TurnoUsuarioId { get; set; }
        public bool EsMiTurno { get; set; }
        public string? GanadorId { get; set; }
        public MotivoFinPartida? MotivoFin { get; set; }
        public CeldaEstado[,] Celdas { get; set; } = new CeldaEstado[12, 12];
    }
}