using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Battleship;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Interfaces.Services
{
    // Reglas de Battleship. Todas las operaciones reciben el id del usuario logueado
    // y validan en el servidor que forme parte de la partida.
    // Fila y columna empiezan en 0 (el tablero es de 12x12).
    public interface IBattleshipService
    {
        // Listados de la pantalla principal
        Task<List<GameListItemDto>> GetActiveGamesAsync(string userId);
        Task<GameHistoryDto> GetHistoryAsync(string userId);
        Task<List<string>> GetActiveOpponentIdsAsync(string userId);

        // Crear partida y revisar a qué pantalla debe ir el usuario
        Task<OperationResult<int>> CreateGameAsync(string creatorId, string opponentId);
        Task<OperationResult<PasoPartida>> GetEntryStepAsync(int gameId, string userId);

        // Fase 1: posicionamiento
        Task<OperationResult<PlacementStatusDto>> GetPlacementStatusAsync(int gameId, string userId);
        Task<OperationResult> PlaceShipAsync(int gameId, string userId, int shipId, int row, int column, DireccionBarco direction);

        // Fase 2: ataque
        Task<OperationResult<AttackBoardDto>> GetAttackBoardAsync(int gameId, string userId);
        Task<OperationResult<AttackResultDto>> AttackAsync(int gameId, string userId, int row, int column);
        Task<OperationResult<CeldaEstado[,]>> GetMyBoardAsync(int gameId, string userId);
        Task<OperationResult> SurrenderAsync(int gameId, string userId);

        // Partida finalizada
        Task<OperationResult<GameResultDto>> GetResultAsync(int gameId, string userId);
    }
}