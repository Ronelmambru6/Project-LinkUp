using LinkUpPro.Domain.Common;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Domain.Entities
{
    // Tamaños por jugador: 2, 3, 3, 4 y 5. Mientras no esté posicionado,
    // la fila, la columna y la dirección quedan en null.
    public class Ship : BaseEntity
    {
        public int GameId { get; set; }
        public string JugadorId { get; set; } = string.Empty;
        public int Tamano { get; set; }
        public int? FilaInicio { get; set; }
        public int? ColumnaInicio { get; set; }
        public DireccionBarco? Direccion { get; set; }
        public bool Posicionado { get; set; }

        public Game? Game { get; set; }
    }
}