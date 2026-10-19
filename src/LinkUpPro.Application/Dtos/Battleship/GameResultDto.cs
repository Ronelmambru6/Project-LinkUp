namespace LinkUpPro.Application.Dtos.Battleship
{
    // Resultado de una partida finalizada, con los tres tableros que pide el enunciado.
    public class GameResultDto
    {
        public int GameId { get; set; }
        public string OponenteId { get; set; } = string.Empty;
        public string GanadorId { get; set; } = string.Empty;
        public bool Gane { get; set; }
        public CeldaEstado[,] MiTableroAtaque { get; set; } = new CeldaEstado[12, 12];
        public CeldaEstado[,] TableroAtaqueOponente { get; set; } = new CeldaEstado[12, 12];
        public CeldaEstado[,] MiTableroPosicionamiento { get; set; } = new CeldaEstado[12, 12];
    }
}