using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Battleship;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    public class BattleshipService : IBattleshipService
    {
        private const string GameNotFoundMessage = "La partida seleccionada no existe o no le pertenece.";
        private const string ConflictMessage = "La partida cambió mientras realizaba la acción. Actualice la pantalla e intente de nuevo.";
        private const string InvalidCellMessage = "La celda seleccionada no es válida.";

        private readonly IGenericRepository<Game> _games;
        private readonly IGenericRepository<Ship> _ships;
        private readonly IGenericRepository<Move> _moves;
        private readonly IGenericRepository<Friendship> _friendships;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserQueryRepository _users;
        private readonly IUnitOfWork _unitOfWork;

        public BattleshipService(
            IGenericRepository<Game> games,
            IGenericRepository<Ship> ships,
            IGenericRepository<Move> moves,
            IGenericRepository<Friendship> friendships,
            IFriendshipRepository friendshipRepository,
            IUserQueryRepository users,
            IUnitOfWork unitOfWork)
        {
            _games = games;
            _ships = ships;
            _moves = moves;
            _friendships = friendships;
            _friendshipRepository = friendshipRepository;
            _users = users;
            _unitOfWork = unitOfWork;
        }

        // ---------------------------------------------------------------
        // Listados
        // ---------------------------------------------------------------

        public async Task<List<GameListItemDto>> GetActiveGamesAsync(string userId)
        {
            var games = await _games.FindAsync(g =>
                g.Estado != EstadoPartida.Finalizada &&
                (g.CreadorId == userId || g.OponenteId == userId));

            // El abandono se revisa al entrar al módulo
            var changed = false;
            foreach (var game in games)
            {
                if (ApplyAbandonment(game))
                {
                    _games.Update(game);
                    changed = true;
                }
            }
            if (changed) await TrySaveAsync();

            var active = games.Where(g => g.Estado != EstadoPartida.Finalizada).ToList();
            var gameIds = active.Select(g => g.Id).ToList();
            var pendingShips = await _ships.FindAsync(s =>
                gameIds.Contains(s.GameId) && s.JugadorId == userId && !s.Posicionado);
            var gamesWithPendingShips = pendingShips.Select(s => s.GameId).ToHashSet();

            return active
                .OrderByDescending(g => g.FechaInicio)
                .Select(g => new GameListItemDto
                {
                    GameId = g.Id,
                    OponenteId = OpponentOf(g, userId),
                    FechaInicio = g.FechaInicio,
                    HorasTranscurridas = (DateTime.UtcNow - g.FechaInicio).TotalHours,
                    Estado = g.Estado,
                    EsMiTurno = g.Estado == EstadoPartida.EnCurso && g.TurnoUsuarioId == userId,
                    PendientePosicionar = gamesWithPendingShips.Contains(g.Id)
                })
                .ToList();
        }

        public async Task<GameHistoryDto> GetHistoryAsync(string userId)
        {
            var games = await _games.FindAsync(g =>
                g.Estado == EstadoPartida.Finalizada &&
                (g.CreadorId == userId || g.OponenteId == userId));

            var items = games
                .Where(g => g.FechaFin.HasValue && g.GanadorId != null)
                .OrderByDescending(g => g.FechaFin)
                .Select(g => new GameHistoryItemDto
                {
                    GameId = g.Id,
                    OponenteId = OpponentOf(g, userId),
                    FechaInicio = g.FechaInicio,
                    FechaFin = g.FechaFin!.Value,
                    DuracionHoras = (g.FechaFin!.Value - g.FechaInicio).TotalHours,
                    Gane = g.GanadorId == userId,
                    GanadorId = g.GanadorId!,
                    MotivoFin = g.MotivoFin
                })
                .ToList();

            return new GameHistoryDto
            {
                Total = items.Count,
                Ganadas = items.Count(i => i.Gane),
                Perdidas = items.Count(i => !i.Gane),
                Partidas = items
            };
        }

        // Amigos con los que ya hay una partida activa: no deben mostrarse al iniciar una nueva.
        public async Task<List<string>> GetActiveOpponentIdsAsync(string userId)
        {
            var games = await _games.FindAsync(g =>
                g.Estado != EstadoPartida.Finalizada &&
                (g.CreadorId == userId || g.OponenteId == userId));

            return games.Select(g => OpponentOf(g, userId)).Distinct().ToList();
        }

        // Amigos activos con los que se puede iniciar una partida nueva (sin partida activa).
        public async Task<List<UserSummaryDto>> GetAvailableOpponentsAsync(string userId)
        {
            var friendIds = await _friendshipRepository.GetActiveFriendIdsAsync(userId);
            var busyIds = await GetActiveOpponentIdsAsync(userId);

            var availableIds = friendIds.Except(busyIds).ToList();
            if (availableIds.Count == 0) return new List<UserSummaryDto>();

            var users = await _users.GetActiveByIdsAsync(availableIds);
            return users.OrderBy(u => u.UserName).ToList();
        }

        // Datos de los jugadores para mostrar nombre y foto en los listados.
        public async Task<Dictionary<string, UserSummaryDto>> GetPlayersAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<string, UserSummaryDto>();

            var users = await _users.GetActiveByIdsAsync(ids);
            return users.ToDictionary(u => u.Id);
        }

        // ---------------------------------------------------------------
        // Crear partida y punto de entrada
        // ---------------------------------------------------------------

        public async Task<OperationResult<int>> CreateGameAsync(string creatorId, string opponentId)
        {
            if (string.IsNullOrWhiteSpace(opponentId))
                return OperationResult<int>.Fail("Debe seleccionar un amigo para iniciar la partida.");

            if (creatorId == opponentId)
                return OperationResult<int>.Fail("No puede iniciar una partida contra usted mismo.");

            var areFriends = await _friendships.ExistsAsync(f =>
                f.Estado == EstadoAmistad.Activa &&
                ((f.UsuarioAId == creatorId && f.UsuarioBId == opponentId) ||
                 (f.UsuarioAId == opponentId && f.UsuarioBId == creatorId)));
            if (!areFriends)
                return OperationResult<int>.Fail("Solo puede iniciar partidas con sus amigos.");

            var hasActiveGame = await _games.ExistsAsync(g =>
                g.Estado != EstadoPartida.Finalizada &&
                ((g.CreadorId == creatorId && g.OponenteId == opponentId) ||
                 (g.CreadorId == opponentId && g.OponenteId == creatorId)));
            if (hasActiveGame)
                return OperationResult<int>.Fail("Ya tiene una partida activa con este amigo.");

            var game = new Game
            {
                CreadorId = creatorId,
                OponenteId = opponentId,
                Estado = EstadoPartida.Posicionando,
                FechaInicio = DateTime.UtcNow
            };

            // Cada jugador recibe sus 5 barcos, todavía sin posicionar
            foreach (var playerId in new[] { creatorId, opponentId })
            {
                foreach (var size in BattleshipRules.ShipSizes)
                {
                    game.Ships.Add(new Ship { JugadorId = playerId, Tamano = size, Posicionado = false });
                }
            }

            await _games.AddAsync(game);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult<int>.Ok(game.Id);
        }

        public async Task<OperationResult<PasoPartida>> GetEntryStepAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<PasoPartida>.Fail(error!);

            await RefreshAbandonmentAsync(game);

            if (game.Estado == EstadoPartida.Finalizada)
                return OperationResult<PasoPartida>.Ok(PasoPartida.Finalizada);

            if (game.Estado == EstadoPartida.EnCurso)
                return OperationResult<PasoPartida>.Ok(PasoPartida.Atacar);

            var hasPendingShips = await _ships.ExistsAsync(s =>
                s.GameId == gameId && s.JugadorId == userId && !s.Posicionado);

            return OperationResult<PasoPartida>.Ok(
                hasPendingShips ? PasoPartida.Posicionar : PasoPartida.EsperandoOponente);
        }

        // ---------------------------------------------------------------
        // Fase 1: posicionamiento
        // ---------------------------------------------------------------

        public async Task<OperationResult<PlacementStatusDto>> GetPlacementStatusAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<PlacementStatusDto>.Fail(error!);

            await RefreshAbandonmentAsync(game);

            var opponentId = OpponentOf(game, userId);
            var ships = await _ships.FindAsync(s => s.GameId == gameId);
            var mine = ships.Where(s => s.JugadorId == userId).ToList();

            return OperationResult<PlacementStatusDto>.Ok(new PlacementStatusDto
            {
                GameId = game.Id,
                OponenteId = opponentId,
                Estado = game.Estado,
                EsCreador = game.CreadorId == userId,
                BarcosPendientes = mine
                    .Where(s => !s.Posicionado)
                    .OrderBy(s => s.Tamano)
                    .ThenBy(s => s.Id)
                    .Select(s => new PendingShipDto { ShipId = s.Id, Tamano = s.Tamano })
                    .ToList(),
                OponentePendiente = ships.Any(s => s.JugadorId == opponentId && !s.Posicionado),
                Tablero = BuildShipsBoard(mine)
            });
        }

        public async Task<OperationResult> PlaceShipAsync(
            int gameId, string userId, int shipId, int row, int column, DireccionBarco direction)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult.Fail(error!);

            if (game.Estado == EstadoPartida.Finalizada)
                return OperationResult.Fail("La partida ya finalizó.");

            if (game.Estado != EstadoPartida.Posicionando)
                return OperationResult.Fail("Los barcos solo se pueden posicionar antes de que empiece la fase de ataque.");

            var ships = await _ships.FindAsync(s => s.GameId == gameId);
            var mine = ships.Where(s => s.JugadorId == userId).ToList();

            var ship = mine.FirstOrDefault(s => s.Id == shipId);
            if (ship is null)
                return OperationResult.Fail("El barco seleccionado no existe.");

            if (ship.Posicionado)
                return OperationResult.Fail("El barco seleccionado ya fue posicionado.");

            if (!BattleshipRules.IsInsideBoard(row, column))
                return OperationResult.Fail(InvalidCellMessage);

            var occupied = mine
                .Where(s => s.Posicionado)
                .SelectMany(s => CellsOf(s))
                .ToHashSet();

            if (occupied.Contains((row, column)))
                return OperationResult.Fail("La celda seleccionada ya está ocupada por otro barco.");

            var cells = BattleshipRules.GetShipCells(row, column, ship.Tamano, direction);

            if (cells.Any(c => !BattleshipRules.IsInsideBoard(c.Row, c.Column)))
                return OperationResult.Fail(
                    "Debe cambiar la celda seleccionada o la dirección, ya que con la combinación actual parte del barco quedaría fuera del tablero.");

            if (cells.Any(c => occupied.Contains(c)))
                return OperationResult.Fail(
                    "Debe cambiar la celda seleccionada o la dirección, ya que con la combinación actual el barco quedaría posicionado encima de otro barco.");

            ship.FilaInicio = row;
            ship.ColumnaInicio = column;
            ship.Direccion = direction;
            ship.Posicionado = true;

            // La fase de ataque empieza cuando los 10 barcos están posicionados; ataca primero el creador
            if (ships.All(s => s.Posicionado))
            {
                game.Estado = EstadoPartida.EnCurso;
                game.TurnoUsuarioId = game.CreadorId;
                game.FechaAsignacionTurno = DateTime.UtcNow;
            }

            // Se actualiza la partida en cada posicionamiento para que RowVersion detecte dos
            // jugadores colocando su último barco a la vez; si no, la partida podría no arrancar nunca.
            _games.Update(game);

            if (!await TrySaveAsync())
                return OperationResult.Fail(ConflictMessage);

            return OperationResult.Ok();
        }

        // ---------------------------------------------------------------
        // Fase 2: ataque
        // ---------------------------------------------------------------

        public async Task<OperationResult<AttackBoardDto>> GetAttackBoardAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<AttackBoardDto>.Fail(error!);

            await RefreshAbandonmentAsync(game);

            var myMoves = await _moves.FindAsync(m => m.GameId == gameId && m.AtacanteId == userId);

            return OperationResult<AttackBoardDto>.Ok(new AttackBoardDto
            {
                GameId = game.Id,
                OponenteId = OpponentOf(game, userId),
                Estado = game.Estado,
                TurnoUsuarioId = game.TurnoUsuarioId,
                EsMiTurno = game.Estado == EstadoPartida.EnCurso && game.TurnoUsuarioId == userId,
                GanadorId = game.GanadorId,
                MotivoFin = game.MotivoFin,
                Celdas = BuildAttackBoard(myMoves)
            });
        }

        public async Task<OperationResult<AttackResultDto>> AttackAsync(int gameId, string userId, int row, int column)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<AttackResultDto>.Fail(error!);

            if (ApplyAbandonment(game))
            {
                _games.Update(game);
                if (!await TrySaveAsync())
                    return OperationResult<AttackResultDto>.Fail(ConflictMessage);
            }

            if (game.Estado == EstadoPartida.Finalizada)
                return OperationResult<AttackResultDto>.Fail("La partida ya finalizó.");

            if (game.Estado != EstadoPartida.EnCurso)
                return OperationResult<AttackResultDto>.Fail(
                    "La fase de ataque comenzará cuando ambos jugadores terminen de posicionar sus barcos.");

            // El turno se valida siempre en el servidor
            if (game.TurnoUsuarioId != userId)
                return OperationResult<AttackResultDto>.Fail("No es su turno de atacar.");

            if (!BattleshipRules.IsInsideBoard(row, column))
                return OperationResult<AttackResultDto>.Fail(InvalidCellMessage);

            var myMoves = await _moves.FindAsync(m => m.GameId == gameId && m.AtacanteId == userId);

            // Atacar una celda ya atacada no hace nada: no se registra y el turno no cambia
            if (myMoves.Any(m => m.Fila == row && m.Columna == column))
                return OperationResult<AttackResultDto>.Ok(new AttackResultDto { YaAtacada = true });

            var opponentId = OpponentOf(game, userId);
            var opponentShips = await _ships.FindAsync(s =>
                s.GameId == gameId && s.JugadorId == opponentId && s.Posicionado);

            var hitShip = opponentShips.FirstOrDefault(s => CellsOf(s).Contains((row, column)));
            var isHit = hitShip is not null;

            await _moves.AddAsync(new Move
            {
                GameId = gameId,
                AtacanteId = userId,
                Fila = row,
                Columna = column,
                Acierto = isHit,
                Fecha = DateTime.UtcNow
            });

            var result = new AttackResultDto { Acierto = isHit };

            if (hitShip is not null)
            {
                var hits = myMoves
                    .Where(m => m.Acierto)
                    .Select(m => (Row: m.Fila, Column: m.Columna))
                    .ToHashSet();
                hits.Add((row, column));

                // Un barco se hunde cuando se aciertan todas sus posiciones
                if (CellsOf(hitShip).All(c => hits.Contains(c)))
                {
                    result.TamanoBarcoHundido = hitShip.Tamano;

                    // Gana quien hunde todos los barcos del oponente
                    if (opponentShips.All(s => CellsOf(s).All(c => hits.Contains(c))))
                    {
                        FinishGame(game, userId, MotivoFinPartida.Hundidos);
                        result.PartidaFinalizada = true;
                        result.GanadorId = userId;
                    }
                }
            }

            // El turno siempre alterna, acierte o falle
            if (!result.PartidaFinalizada)
            {
                game.TurnoUsuarioId = opponentId;
                game.FechaAsignacionTurno = DateTime.UtcNow;
            }

            _games.Update(game);

            if (!await TrySaveAsync())
                return OperationResult<AttackResultDto>.Fail(ConflictMessage);

            return OperationResult<AttackResultDto>.Ok(result);
        }

        public async Task<OperationResult<CeldaEstado[,]>> GetMyBoardAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<CeldaEstado[,]>.Fail(error!);

            var ships = await _ships.FindAsync(s => s.GameId == gameId && s.JugadorId == userId);
            return OperationResult<CeldaEstado[,]>.Ok(BuildShipsBoard(ships));
        }

        public async Task<OperationResult> SurrenderAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult.Fail(error!);

            if (ApplyAbandonment(game))
            {
                _games.Update(game);
                await TrySaveAsync();
            }

            if (game.Estado == EstadoPartida.Finalizada)
                return OperationResult.Fail("La partida ya finalizó.");

            FinishGame(game, OpponentOf(game, userId), MotivoFinPartida.Rendicion);
            _games.Update(game);

            if (!await TrySaveAsync())
                return OperationResult.Fail(ConflictMessage);

            return OperationResult.Ok();
        }

        // ---------------------------------------------------------------
        // Partida finalizada
        // ---------------------------------------------------------------

        public async Task<OperationResult<GameResultDto>> GetResultAsync(int gameId, string userId)
        {
            var (game, error) = await GetGameForParticipantAsync(gameId, userId);
            if (game is null) return OperationResult<GameResultDto>.Fail(error!);

            await RefreshAbandonmentAsync(game);

            // Los tableros del oponente solo se pueden ver cuando la partida ya terminó
            if (game.Estado != EstadoPartida.Finalizada || game.GanadorId is null)
                return OperationResult<GameResultDto>.Fail("La partida todavía no ha finalizado.");

            var opponentId = OpponentOf(game, userId);
            var moves = await _moves.FindAsync(m => m.GameId == gameId);
            var myShips = await _ships.FindAsync(s => s.GameId == gameId && s.JugadorId == userId);

            return OperationResult<GameResultDto>.Ok(new GameResultDto
            {
                GameId = game.Id,
                OponenteId = opponentId,
                GanadorId = game.GanadorId,
                Gane = game.GanadorId == userId,
                MiTableroAtaque = BuildAttackBoard(moves.Where(m => m.AtacanteId == userId)),
                TableroAtaqueOponente = BuildAttackBoard(moves.Where(m => m.AtacanteId == opponentId)),
                MiTableroPosicionamiento = BuildShipsBoard(myShips)
            });
        }

        // ---------------------------------------------------------------
        // Ayudas internas
        // ---------------------------------------------------------------

        private async Task<(Game? Game, string? Error)> GetGameForParticipantAsync(int gameId, string userId)
        {
            var game = await _games.GetByIdAsync(gameId);
            if (game is null || (game.CreadorId != userId && game.OponenteId != userId))
                return (null, GameNotFoundMessage);

            return (game, null);
        }

        private static string OpponentOf(Game game, string userId) =>
            game.CreadorId == userId ? game.OponenteId : game.CreadorId;

        // Si pasaron 48 horas desde que se asignó el turno sin atacar, gana el oponente.
        // El posicionamiento no vence. Devuelve true si la partida cambió.
        private static bool ApplyAbandonment(Game game)
        {
            if (game.Estado != EstadoPartida.EnCurso ||
                game.FechaAsignacionTurno is null ||
                game.TurnoUsuarioId is null)
                return false;

            if (DateTime.UtcNow - game.FechaAsignacionTurno.Value < BattleshipRules.AbandonmentLimit)
                return false;

            FinishGame(game, OpponentOf(game, game.TurnoUsuarioId), MotivoFinPartida.Abandono);
            return true;
        }

        private async Task RefreshAbandonmentAsync(Game game)
        {
            if (ApplyAbandonment(game))
            {
                _games.Update(game);
                await TrySaveAsync();
            }
        }

        private static void FinishGame(Game game, string winnerId, MotivoFinPartida reason)
        {
            game.Estado = EstadoPartida.Finalizada;
            game.GanadorId = winnerId;
            game.MotivoFin = reason;
            game.FechaFin = DateTime.UtcNow;
            game.TurnoUsuarioId = null;
            game.FechaAsignacionTurno = null;
        }

        // false si otra operación modificó la partida al mismo tiempo
        private async Task<bool> TrySaveAsync()
        {
            try
            {
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (ConcurrencyException)
            {
                return false;
            }
        }

        private static List<(int Row, int Column)> CellsOf(Ship ship)
        {
            if (ship.FilaInicio is int row && ship.ColumnaInicio is int column && ship.Direccion is DireccionBarco direction)
                return BattleshipRules.GetShipCells(row, column, ship.Tamano, direction);

            return new List<(int Row, int Column)>();
        }

        private static CeldaEstado[,] BuildShipsBoard(IEnumerable<Ship> ships)
        {
            var board = new CeldaEstado[BattleshipRules.BoardSize, BattleshipRules.BoardSize];
            foreach (var ship in ships.Where(s => s.Posicionado))
            {
                foreach (var (row, column) in CellsOf(ship))
                {
                    if (BattleshipRules.IsInsideBoard(row, column))
                        board[row, column] = CeldaEstado.Barco;
                }
            }
            return board;
        }

        private static CeldaEstado[,] BuildAttackBoard(IEnumerable<Move> moves)
        {
            var board = new CeldaEstado[BattleshipRules.BoardSize, BattleshipRules.BoardSize];
            foreach (var move in moves)
            {
                if (BattleshipRules.IsInsideBoard(move.Fila, move.Columna))
                    board[move.Fila, move.Columna] = move.Acierto ? CeldaEstado.Acierto : CeldaEstado.Fallo;
            }
            return board;
        }
    }
}