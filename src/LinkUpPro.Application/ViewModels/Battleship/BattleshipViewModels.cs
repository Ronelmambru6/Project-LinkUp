using LinkUpPro.Application.Dtos.Battleship;
using LinkUpPro.Application.ViewModels.Users;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.Battleship
{
    // Cómo se dibuja un tablero y qué hacen sus celdas al hacer clic.
    public enum ModoTablero
    {
        SoloLectura = 0,       // sin acciones (resultado, mi tablero, espera)
        SeleccionarCelda = 1,  // posicionamiento: las celdas libres llevan a elegir la dirección
        Atacar = 2             // ataque: las celdas sin atacar envían el ataque (solo en mi turno)
    }

    public class TableroViewModel
    {
        public int GameId { get; set; }
        public CeldaEstado[,] Celdas { get; set; } = new CeldaEstado[12, 12];
        public ModoTablero Modo { get; set; } = ModoTablero.SoloLectura;
        public int? ShipId { get; set; }
        public string Titulo { get; set; } = string.Empty;
    }

    public class PartidaActivaViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public DateTime FechaInicio { get; set; }
        public double HorasTranscurridas { get; set; }
        public bool EsMiTurno { get; set; }
        public bool PendientePosicionar { get; set; }
        public EstadoPartida Estado { get; set; }
    }

    public class PartidaHistorialViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public double DuracionHoras { get; set; }
        public bool Gane { get; set; }
        public string Ganador { get; set; } = string.Empty;
    }

    public class GameIndexViewModel
    {
        public List<PartidaActivaViewModel> Activas { get; set; } = new();
        public List<PartidaHistorialViewModel> Historial { get; set; } = new();
        public int Total { get; set; }
        public int Ganadas { get; set; }
        public int Perdidas { get; set; }
    }

    public class NuevaPartidaViewModel
    {
        public List<UserSummaryViewModel> Amigos { get; set; } = new();
    }

    public class PosicionarViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public List<PendingShipDto> BarcosPendientes { get; set; } = new();
    }

    public class DireccionViewModel
    {
        public int GameId { get; set; }
        public int ShipId { get; set; }
        public int Tamano { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public DireccionBarco? Direccion { get; set; }

        public string CeldaTexto => $"{(char)('A' + Fila)}{Columna + 1}";
    }

    public class EsperaViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public TableroViewModel Tablero { get; set; } = new();
    }

    public class AtaqueViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public bool EsMiTurno { get; set; }
        public TableroViewModel Tablero { get; set; } = new();
    }

    public class ResultadoViewModel
    {
        public UserSummaryViewModel Oponente { get; set; } = new();
        public bool Gane { get; set; }
        public string Ganador { get; set; } = string.Empty;
        public TableroViewModel MiAtaque { get; set; } = new();
        public TableroViewModel AtaqueOponente { get; set; } = new();
        public TableroViewModel MiPosicionamiento { get; set; } = new();
    }

    public class RendirseViewModel
    {
        public int GameId { get; set; }
        public UserSummaryViewModel Oponente { get; set; } = new();
        public string Origen { get; set; } = "lista";
    }
}
