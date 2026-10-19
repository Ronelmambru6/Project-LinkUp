using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    // Reglas fijas de Battleship. Las coordenadas empiezan en 0 (fila 0 = primera fila).
    public static class BattleshipRules
    {
        public const int BoardSize = 12;
        public static readonly IReadOnlyList<int> ShipSizes = new[] { 2, 3, 3, 4, 5 };
        public static readonly TimeSpan AbandonmentLimit = TimeSpan.FromHours(48);

        public static bool IsInsideBoard(int row, int column) =>
            row >= 0 && row < BoardSize && column >= 0 && column < BoardSize;

        // Celdas que ocuparía un barco. Pueden quedar fuera del tablero:
        // quien llama debe validarlo con IsInsideBoard.
        public static List<(int Row, int Column)> GetShipCells(int startRow, int startColumn, int size, DireccionBarco direction)
        {
            var (rowStep, columnStep) = direction switch
            {
                DireccionBarco.Arriba => (-1, 0),
                DireccionBarco.Abajo => (1, 0),
                DireccionBarco.Izquierda => (0, -1),
                _ => (0, 1)
            };

            var cells = new List<(int Row, int Column)>(size);
            for (var i = 0; i < size; i++)
            {
                cells.Add((startRow + rowStep * i, startColumn + columnStep * i));
            }
            return cells;
        }
    }
}