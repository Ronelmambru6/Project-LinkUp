using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Battleship
{
    public class GameListItemDto
    {
        public int GameId { get; set; }
        public string OponenteId { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public double HorasTranscurridas { get; set; }
        public EstadoPartida Estado { get; set; }
        public bool EsMiTurno { get; set; }
        public bool PendientePosicionar { get; set; }
    }
}