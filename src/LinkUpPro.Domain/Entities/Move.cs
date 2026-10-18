using LinkUpPro.Domain.Common;

namespace LinkUpPro.Domain.Entities
{
    // Ataque realizado por un jugador sobre una celda del tablero del rival.
    public class Move : BaseEntity
    {
        public int GameId { get; set; }
        public string AtacanteId { get; set; } = string.Empty;
        public int Fila { get; set; }
        public int Columna { get; set; }
        public bool Acierto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public Game? Game { get; set; }
    }
}