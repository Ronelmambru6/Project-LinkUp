using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Battleship
{
    // Estado de la fase de posicionamiento para el usuario logueado.
    public class PlacementStatusDto
    {
        public int GameId { get; set; }
        public string OponenteId { get; set; } = string.Empty;
        public EstadoPartida Estado { get; set; }
        public bool EsCreador { get; set; }
        public List<PendingShipDto> BarcosPendientes { get; set; } = new();
        public bool OponentePendiente { get; set; }
        public CeldaEstado[,] Tablero { get; set; } = new CeldaEstado[12, 12];
    }
}