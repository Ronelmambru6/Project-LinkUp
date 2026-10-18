using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Battleship
{
    public class GameHistoryItemDto
    {
        public int GameId { get; set; }
        public string OponenteId { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public double DuracionHoras { get; set; }
        public bool Gane { get; set; }
        public string GanadorId { get; set; } = string.Empty;
        public MotivoFinPartida? MotivoFin { get; set; }
    }
}