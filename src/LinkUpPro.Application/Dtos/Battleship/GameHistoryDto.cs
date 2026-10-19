namespace LinkUpPro.Application.Dtos.Battleship
{
    public class GameHistoryDto
    {
        public int Total { get; set; }
        public int Ganadas { get; set; }
        public int Perdidas { get; set; }
        public List<GameHistoryItemDto> Partidas { get; set; } = new();
    }
}