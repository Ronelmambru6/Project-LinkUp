namespace LinkUpPro.Application.Dtos.Battleship
{
    public class AttackResultDto
    {
        // true si la celda ya había sido atacada: no se registra nada y el turno no cambia.
        public bool YaAtacada { get; set; }
        public bool Acierto { get; set; }
        public int? TamanoBarcoHundido { get; set; }
        public bool PartidaFinalizada { get; set; }
        public string? GanadorId { get; set; }
    }
}