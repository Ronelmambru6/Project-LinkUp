using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Battleship;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Battleship;
using LinkUpPro.Application.ViewModels.Users;
using LinkUpPro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly IBattleshipService _battleship;
        private readonly IMapper _mapper;

        public GameController(IBattleshipService battleship, IMapper mapper)
        {
            _battleship = battleship;
            _mapper = mapper;
        }

        // El Id del usuario sale siempre de la sesión; el servicio valida que sea parte de la partida
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ───────────────────────────── Pantalla principal ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var activas = await _battleship.GetActiveGamesAsync(UserId);
            var historial = await _battleship.GetHistoryAsync(UserId);
            var jugadores = await _battleship.GetPlayersAsync(
                activas.Select(a => a.OponenteId).Concat(historial.Partidas.Select(p => p.OponenteId)));

            var modelo = new GameIndexViewModel
            {
                Total = historial.Total,
                Ganadas = historial.Ganadas,
                Perdidas = historial.Perdidas,
                Activas = activas.Select(a => new PartidaActivaViewModel
                {
                    GameId = a.GameId,
                    Oponente = Jugador(jugadores, a.OponenteId),
                    FechaInicio = a.FechaInicio,
                    HorasTranscurridas = a.HorasTranscurridas,
                    EsMiTurno = a.EsMiTurno,
                    PendientePosicionar = a.PendientePosicionar,
                    Estado = a.Estado
                }).ToList(),
                Historial = historial.Partidas.Select(p =>
                {
                    var oponente = Jugador(jugadores, p.OponenteId);
                    return new PartidaHistorialViewModel
                    {
                        GameId = p.GameId,
                        Oponente = oponente,
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        DuracionHoras = p.DuracionHoras,
                        Gane = p.Gane,
                        Ganador = p.GanadorId == UserId ? "Yo" : oponente.UserName
                    };
                }).ToList()
            };

            return View(modelo);
        }

        // ───────────────────────────── Nueva partida ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> New(string? search)
        {
            var amigos = await _battleship.GetAvailableOpponentsAsync(UserId, search);
            return View(new NuevaPartidaViewModel
            {
                Search = search?.Trim(),
                Amigos = _mapper.Map<List<UserSummaryViewModel>>(amigos)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string? oponenteId)
        {
            var resultado = await _battleship.CreateGameAsync(UserId, oponenteId ?? string.Empty);
            if (!resultado.Succeeded)
            {
                TempData["Error"] = resultado.Message;
                return RedirectToAction(nameof(New));
            }

            TempData["Success"] = "La partida fue creada correctamente. Posicione sus barcos para continuar.";
            return RedirectToAction(nameof(Enter), new { id = resultado.Data });
        }

        // ───────────────────────────── Entrar a una partida ─────────────────────────────

        // Decide a qué pantalla va el usuario según el estado real de la partida.
        // Si el invitado entra sin barcos posicionados, termina en la selección de barcos.
        [HttpGet]
        public async Task<IActionResult> Enter(int id)
        {
            var paso = await _battleship.GetEntryStepAsync(id, UserId);
            if (!paso.Succeeded)
                return VolverConError(paso.Message);

            return paso.Data switch
            {
                PasoPartida.Posicionar => RedirectToAction(nameof(Place), new { id }),
                PasoPartida.EsperandoOponente => RedirectToAction(nameof(Waiting), new { id }),
                PasoPartida.Atacar => RedirectToAction(nameof(Attack), new { id }),
                _ => RedirectToAction(nameof(Result), new { id })
            };
        }

        // ───────────────────────────── Posicionamiento ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Place(int id)
        {
            var estado = await _battleship.GetPlacementStatusAsync(id, UserId);
            if (!estado.Succeeded || estado.Data is null)
                return VolverConError(estado.Message);

            if (estado.Data.Estado != EstadoPartida.Posicionando || estado.Data.BarcosPendientes.Count == 0)
                return RedirectToAction(nameof(Enter), new { id });

            var jugadores = await _battleship.GetPlayersAsync(new[] { estado.Data.OponenteId });
            return View(new PosicionarViewModel
            {
                GameId = id,
                Oponente = Jugador(jugadores, estado.Data.OponenteId),
                BarcosPendientes = estado.Data.BarcosPendientes
            });
        }

        // Valida que se haya elegido un barco antes de mostrar el tablero
        [HttpGet]
        public async Task<IActionResult> Board(int id, int? shipId)
        {
            if (shipId is null)
            {
                TempData["Error"] = "Debe seleccionar un barco para posicionar.";
                return RedirectToAction(nameof(Place), new { id });
            }

            var estado = await _battleship.GetPlacementStatusAsync(id, UserId);
            if (!estado.Succeeded || estado.Data is null)
                return VolverConError(estado.Message);

            var barco = estado.Data.BarcosPendientes.FirstOrDefault(b => b.ShipId == shipId);
            if (barco is null)
            {
                TempData["Error"] = "El barco seleccionado no está disponible para posicionar.";
                return RedirectToAction(nameof(Place), new { id });
            }

            ViewBag.Tamano = barco.Tamano;
            return View(new TableroViewModel
            {
                GameId = id,
                ShipId = barco.ShipId,
                Celdas = estado.Data.Tablero,
                Modo = ModoTablero.SeleccionarCelda,
                Titulo = $"Seleccione la celda inicial del barco de {barco.Tamano} casillas"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Direction(int id, int shipId, int row, int column)
        {
            var (modelo, error) = await CargarDireccionAsync(id, shipId, row, column);
            if (modelo is null)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Board), new { id, shipId });
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Direction(int id, int shipId, int row, int column, DireccionBarco? direccion)
        {
            var (modelo, error) = await CargarDireccionAsync(id, shipId, row, column);
            if (modelo is null)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Board), new { id, shipId });
            }

            if (direccion is null)
            {
                ModelState.AddModelError(nameof(DireccionViewModel.Direccion), "Debe seleccionar una dirección.");
                return View(modelo);
            }

            var resultado = await _battleship.PlaceShipAsync(id, UserId, shipId, row, column, direccion.Value);
            if (!resultado.Succeeded)
            {
                // Fuera del tablero o superposición: se queda en esta pantalla mostrando el error
                modelo.Direccion = direccion;
                ModelState.AddModelError(string.Empty, resultado.Message ?? "No se pudo posicionar el barco.");
                return View(modelo);
            }

            TempData["Success"] = "El barco fue posicionado correctamente.";
            return RedirectToAction(nameof(Enter), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Waiting(int id)
        {
            var estado = await _battleship.GetPlacementStatusAsync(id, UserId);
            if (!estado.Succeeded || estado.Data is null)
                return VolverConError(estado.Message);

            // Si ya hay barcos pendientes o la partida avanzó, esta pantalla no corresponde
            if (estado.Data.Estado != EstadoPartida.Posicionando || estado.Data.BarcosPendientes.Count > 0)
                return RedirectToAction(nameof(Enter), new { id });

            var jugadores = await _battleship.GetPlayersAsync(new[] { estado.Data.OponenteId });
            return View(new EsperaViewModel
            {
                GameId = id,
                Oponente = Jugador(jugadores, estado.Data.OponenteId),
                Tablero = new TableroViewModel
                {
                    GameId = id,
                    Celdas = estado.Data.Tablero,
                    Titulo = "Mi tablero"
                }
            });
        }

        // ───────────────────────────── Ataque ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Attack(int id)
        {
            var tablero = await _battleship.GetAttackBoardAsync(id, UserId);
            if (!tablero.Succeeded || tablero.Data is null)
                return VolverConError(tablero.Message);

            if (tablero.Data.Estado != EstadoPartida.EnCurso)
                return RedirectToAction(nameof(Enter), new { id });

            var jugadores = await _battleship.GetPlayersAsync(new[] { tablero.Data.OponenteId });
            return View(new AtaqueViewModel
            {
                GameId = id,
                Oponente = Jugador(jugadores, tablero.Data.OponenteId),
                EsMiTurno = tablero.Data.EsMiTurno,
                Tablero = new TableroViewModel
                {
                    GameId = id,
                    Celdas = tablero.Data.Celdas,
                    // Fuera de turno el tablero queda sin acciones: el clic no hace nada
                    Modo = tablero.Data.EsMiTurno ? ModoTablero.Atacar : ModoTablero.SoloLectura,
                    Titulo = "Tablero de ataque"
                }
            });
        }

        // La celda llega como "fila,columna"; el servidor valida turno, rango y celda repetida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attack(int id, string? celda)
        {
            var partes = (celda ?? string.Empty).Split(',');
            if (partes.Length != 2 || !int.TryParse(partes[0], out var fila) || !int.TryParse(partes[1], out var columna))
            {
                TempData["Error"] = "La celda seleccionada no es válida.";
                return RedirectToAction(nameof(Attack), new { id });
            }

            var resultado = await _battleship.AttackAsync(id, UserId, fila, columna);
            if (!resultado.Succeeded || resultado.Data is null)
            {
                TempData["Error"] = resultado.Message;
                return RedirectToAction(nameof(Attack), new { id });
            }

            var ataque = resultado.Data;
            if (ataque.PartidaFinalizada)
                return RedirectToAction(nameof(Result), new { id });

            if (!ataque.YaAtacada)
            {
                TempData["Success"] = ataque.Acierto
                    ? (ataque.TamanoBarcoHundido is { } tamano
                        ? $"¡Acierto! Hundió un barco de {tamano} casillas."
                        : "¡Acierto!")
                    : "Fallo. Es el turno de su oponente.";
            }

            return RedirectToAction(nameof(Attack), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> MyBoard(int id)
        {
            var tablero = await _battleship.GetMyBoardAsync(id, UserId);
            if (!tablero.Succeeded || tablero.Data is null)
                return VolverConError(tablero.Message);

            return View(new TableroViewModel
            {
                GameId = id,
                Celdas = tablero.Data,
                Titulo = "Mi tablero"
            });
        }

        // ───────────────────────────── Rendirse ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Surrender(int id, string? origen)
        {
            // Solo se acepta confirmar la rendición de una partida que sigue abierta
            var paso = await _battleship.GetEntryStepAsync(id, UserId);
            if (!paso.Succeeded)
                return VolverConError(paso.Message);

            if (paso.Data == PasoPartida.Finalizada)
                return VolverConError("Esta partida ya finalizó.");

            var tablero = await _battleship.GetAttackBoardAsync(id, UserId);
            if (!tablero.Succeeded || tablero.Data is null)
                return VolverConError(tablero.Message);

            var jugadores = await _battleship.GetPlayersAsync(new[] { tablero.Data.OponenteId });
            return View(new RendirseViewModel
            {
                GameId = id,
                Oponente = Jugador(jugadores, tablero.Data.OponenteId),
                Origen = origen == "ataque" ? "ataque" : "lista"
            });
        }

        [HttpPost, ActionName("Surrender")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SurrenderConfirmed(int id)
        {
            var resultado = await _battleship.SurrenderAsync(id, UserId);
            if (!resultado.Succeeded)
                return VolverConError(resultado.Message);

            TempData["Success"] = "Se rindió en la partida.";
            return RedirectToAction(nameof(Result), new { id });
        }

        // ───────────────────────────── Resultado ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var resultado = await _battleship.GetResultAsync(id, UserId);
            if (!resultado.Succeeded || resultado.Data is null)
                return VolverConError(resultado.Message);

            var datos = resultado.Data;
            var jugadores = await _battleship.GetPlayersAsync(new[] { datos.OponenteId });
            var oponente = Jugador(jugadores, datos.OponenteId);

            return View(new ResultadoViewModel
            {
                Oponente = oponente,
                Gane = datos.Gane,
                Ganador = datos.Gane ? "Yo" : oponente.UserName,
                MiAtaque = new TableroViewModel { GameId = id, Celdas = datos.MiTableroAtaque, Titulo = "Mi tablero de ataque" },
                AtaqueOponente = new TableroViewModel { GameId = id, Celdas = datos.TableroAtaqueOponente, Titulo = $"Tablero de ataque de @{oponente.UserName}" },
                MiPosicionamiento = new TableroViewModel { GameId = id, Celdas = datos.MiTableroPosicionamiento, Titulo = "Mi posicionamiento" }
            });
        }

        // ───────────────────────────── Ayudas ─────────────────────────────

        private async Task<(DireccionViewModel? Modelo, string? Error)> CargarDireccionAsync(int id, int shipId, int row, int column)
        {
            var estado = await _battleship.GetPlacementStatusAsync(id, UserId);
            if (!estado.Succeeded || estado.Data is null)
                return (null, estado.Message);

            var barco = estado.Data.BarcosPendientes.FirstOrDefault(b => b.ShipId == shipId);
            if (barco is null)
                return (null, "El barco seleccionado no está disponible para posicionar.");

            if (row < 0 || row >= 12 || column < 0 || column >= 12 || estado.Data.Tablero[row, column] != CeldaEstado.Vacia)
                return (null, "La celda seleccionada no está disponible.");

            return (new DireccionViewModel
            {
                GameId = id,
                ShipId = shipId,
                Tamano = barco.Tamano,
                Fila = row,
                Columna = column
            }, null);
        }

        private UserSummaryViewModel Jugador(Dictionary<string, UserSummaryDto> jugadores, string id) =>
            jugadores.TryGetValue(id, out var dto)
                ? _mapper.Map<UserSummaryViewModel>(dto)
                : new UserSummaryViewModel { Id = id, UserName = "Usuario no disponible", FotoPerfil = string.Empty };

        private IActionResult VolverConError(string? mensaje)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}
