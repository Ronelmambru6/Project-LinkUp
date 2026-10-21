using LinkUpPro.Application.Common;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.FriendRequests;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Services
{
    // Reglas de negocio de Solicitudes de amistad (enunciado pp. 47–61).
    // Toda operación vuelve a validar el estado actual dentro de una transacción,
    // aunque la solicitud se viera bien cuando el usuario abrió la pantalla.
    public class FriendRequestService : IFriendRequestService
    {
        private const string SinPermiso = "No posee permisos para realizar esta acción sobre la solicitud.";
        private const string YaSonAmigos = "Este usuario ya forma parte de su lista de amigos.";

        private readonly IFriendRequestRepository _requestRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly IUnitOfWork _unitOfWork;

        public FriendRequestService(
            IFriendRequestRepository requestRepository,
            IFriendshipRepository friendshipRepository,
            IUserQueryRepository userQueryRepository,
            IFriendshipService friendshipService,
            IUnitOfWork unitOfWork)
        {
            _requestRepository = requestRepository;
            _friendshipRepository = friendshipRepository;
            _userQueryRepository = userQueryRepository;
            _friendshipService = friendshipService;
            _unitOfWork = unitOfWork;
        }

        // ───────────────────────────── Consultas ─────────────────────────────

        public async Task<List<FriendRequestDto>> GetPendingReceivedAsync(string userId)
        {
            var solicitudes = await _requestRepository.GetPendingReceivedFromActiveAsync(userId);

            // Enunciado: no se muestran si ya existe una amistad activa entre ambos
            var amigos = (await _friendshipRepository.GetActiveFriendIdsAsync(userId)).ToHashSet();
            solicitudes = solicitudes.Where(s => !amigos.Contains(s.EmisorId)).ToList();

            return await ToDtosAsync(userId, solicitudes, s => s.EmisorId);
        }

        public async Task<List<FriendRequestDto>> GetSentAsync(string userId)
        {
            var solicitudes = await _requestRepository.GetSentVisibleToActiveAsync(userId);
            return await ToDtosAsync(userId, solicitudes, s => s.ReceptorId);
        }

        public Task<int> CountPendingReceivedAsync(string userId) =>
            _requestRepository.CountPendingReceivedFromActiveAsync(userId);

        public async Task<List<AvailableUserDto>> GetAvailableUsersAsync(string userId, string? search)
        {
            // Enunciado: activos, distintos de uno mismo, sin amistad activa y sin solicitud pendiente en ninguna dirección.
            // Las amistades eliminadas y las solicitudes rechazadas o canceladas NO impiden aparecer.
            var excluidos = new HashSet<string> { userId };
            excluidos.UnionWith(await _friendshipRepository.GetActiveFriendIdsAsync(userId));
            excluidos.UnionWith(await _requestRepository.GetPendingCounterpartIdsAsync(userId));

            var usuarios = await _userQueryRepository.SearchActiveAsync(search, excluidos);

            var resultado = new List<AvailableUserDto>();
            foreach (var usuario in usuarios)
            {
                resultado.Add(new AvailableUserDto
                {
                    Usuario = usuario,
                    AmigosEnComun = await _friendshipService.CountMutualFriendsAsync(userId, usuario.Id)
                });
            }
            return resultado;
        }

        public async Task<ServiceResult<FriendRequestDto>> GetForConfirmationAsync(int requestId, string userId, FriendRequestAction action)
        {
            var solicitud = await _requestRepository.GetByIdAsync(requestId);
            if (solicitud is null)
                return ServiceResult<FriendRequestDto>.Failure(MensajeNoDisponible(action));

            var error = ValidarParaAccion(solicitud, userId, action);
            if (error is not null)
                return ServiceResult<FriendRequestDto>.Failure(error);

            var otroUsuarioId = solicitud.EmisorId == userId ? solicitud.ReceptorId : solicitud.EmisorId;
            var otroUsuario = await _userQueryRepository.GetActiveByIdAsync(otroUsuarioId);
            if (otroUsuario is null)
                return ServiceResult<FriendRequestDto>.Failure(MensajeInactivo(action));

            return ServiceResult<FriendRequestDto>.Success(new FriendRequestDto
            {
                Id = solicitud.Id,
                Estado = solicitud.Estado,
                FechaEnvio = solicitud.FechaEnvio,
                FechaRespuesta = solicitud.FechaRespuesta,
                OtroUsuario = otroUsuario
            });
        }

        // ───────────────────────────── Enviar ─────────────────────────────

        public async Task<ServiceResult> SendAsync(string senderId, string? receiverId)
        {
            if (string.IsNullOrWhiteSpace(receiverId))
                return ServiceResult.Failure("SelectedUserId", "Debe seleccionar un usuario para enviar la solicitud de amistad.");

            if (receiverId == senderId)
                return ServiceResult.Failure("No puede enviarse una solicitud de amistad a sí mismo.");

            var resultado = ServiceResult.Success();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // No se confía en que el usuario aparecía en el listado: todo se valida de nuevo
                if (!await _userQueryRepository.IsActiveAsync(receiverId) || !await _userQueryRepository.IsActiveAsync(senderId))
                {
                    resultado = ServiceResult.Failure("No se puede enviar la solicitud porque uno de los usuarios se encuentra inactivo.");
                    return;
                }

                if (await _friendshipService.AreFriendsAsync(senderId, receiverId))
                {
                    resultado = ServiceResult.Failure(YaSonAmigos);
                    return;
                }

                var pendientes = await _requestRepository.GetPendingBetweenAsync(senderId, receiverId);
                if (pendientes.Any(p => p.EmisorId == senderId))
                {
                    resultado = ServiceResult.Failure("Ya envió una solicitud de amistad a este usuario y se encuentra pendiente de respuesta.");
                    return;
                }
                if (pendientes.Any())
                {
                    resultado = ServiceResult.Failure("Este usuario ya le envió una solicitud de amistad. Debe aceptarla o rechazarla desde sus solicitudes pendientes.");
                    return;
                }

                // Siempre un registro nuevo: las solicitudes anteriores se conservan como historial
                await _requestRepository.AddAsync(new FriendRequest
                {
                    EmisorId = senderId,
                    ReceptorId = receiverId,
                    Estado = EstadoSolicitud.EnEspera,
                    FechaEnvio = DateTime.UtcNow,
                    OcultaParaEmisor = false
                });
            });

            return resultado;
        }

        // ───────────────────────────── Responder, cancelar y ocultar ─────────────────────────────

        public async Task<ServiceResult> AcceptAsync(int requestId, string userId)
        {
            var resultado = ServiceResult.Success();

            // Enunciado: aceptar y crear/reactivar la amistad es UNA sola operación
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitud = await _requestRepository.GetByIdAsync(requestId);
                var error = await ValidarConCuentasAsync(solicitud, userId, FriendRequestAction.Accept);
                if (error is not null)
                {
                    resultado = ServiceResult.Failure(error);
                    return;
                }

                var (usuarioA, usuarioB) = UserPair.Order(solicitud!.EmisorId, solicitud.ReceptorId);
                var amistad = await _friendshipRepository.GetByPairAsync(usuarioA, usuarioB);

                if (amistad?.Estado == EstadoAmistad.Activa)
                {
                    resultado = ServiceResult.Failure(YaSonAmigos);
                    return;
                }

                solicitud.Estado = EstadoSolicitud.Aceptada;
                solicitud.FechaRespuesta = DateTime.UtcNow;

                if (amistad is null)
                {
                    // Nunca fueron amigos: se crea la relación
                    await _friendshipRepository.AddAsync(new Friendship
                    {
                        UsuarioAId = usuarioA,
                        UsuarioBId = usuarioB,
                        Estado = EstadoAmistad.Activa,
                        FechaCreacion = DateTime.UtcNow
                    });
                }
                else
                {
                    // Fueron amigos antes: se reactiva la misma relación, nunca se duplica
                    amistad.Estado = EstadoAmistad.Activa;
                    amistad.FechaEliminacion = null;
                }
            });

            return resultado;
        }

        public async Task<ServiceResult> RejectAsync(int requestId, string userId)
        {
            var resultado = ServiceResult.Success();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitud = await _requestRepository.GetByIdAsync(requestId);
                var error = await ValidarConCuentasAsync(solicitud, userId, FriendRequestAction.Reject);
                if (error is not null)
                {
                    resultado = ServiceResult.Failure(error);
                    return;
                }

                // Rechazar no crea ni modifica ninguna amistad
                solicitud!.Estado = EstadoSolicitud.Rechazada;
                solicitud.FechaRespuesta = DateTime.UtcNow;
            });

            return resultado;
        }

        public async Task<ServiceResult> CancelAsync(int requestId, string userId)
        {
            var resultado = ServiceResult.Success();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitud = await _requestRepository.GetByIdAsync(requestId);
                var error = await ValidarConCuentasAsync(solicitud, userId, FriendRequestAction.Cancel);
                if (error is not null)
                {
                    resultado = ServiceResult.Failure(error);
                    return;
                }

                // Desaparece de los dos listados y del contador; se conserva internamente
                solicitud!.Estado = EstadoSolicitud.Cancelada;
                solicitud.FechaCancelacion = DateTime.UtcNow;
            });

            return resultado;
        }

        public async Task<ServiceResult> DeleteFromHistoryAsync(int requestId, string userId)
        {
            var resultado = ServiceResult.Success();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var solicitud = await _requestRepository.GetByIdAsync(requestId);
                if (solicitud is null)
                {
                    resultado = ServiceResult.Failure(MensajeNoDisponible(FriendRequestAction.DeleteFromHistory));
                    return;
                }

                var error = ValidarParaAccion(solicitud, userId, FriendRequestAction.DeleteFromHistory);
                if (error is not null)
                {
                    resultado = ServiceResult.Failure(error);
                    return;
                }

                // Borrado lógico: solo cambia el indicador. El estado y la amistad no se tocan.
                solicitud.OcultaParaEmisor = true;
            });

            return resultado;
        }

        // ───────────────────────────── Validaciones ─────────────────────────────

        // Permisos y estado según la acción. Devuelve el mensaje de error o null si es válida.
        private static string? ValidarParaAccion(FriendRequest solicitud, string userId, FriendRequestAction action)
        {
            switch (action)
            {
                case FriendRequestAction.Accept:
                case FriendRequestAction.Reject:
                    // Solo el receptor puede aceptar o rechazar
                    if (solicitud.ReceptorId != userId) return SinPermiso;
                    if (solicitud.Estado != EstadoSolicitud.EnEspera) return MensajeNoDisponible(action);
                    return null;

                case FriendRequestAction.Cancel:
                    // Solo el emisor puede cancelar, y solo mientras esté en espera
                    if (solicitud.EmisorId != userId) return SinPermiso;
                    if (solicitud.Estado != EstadoSolicitud.EnEspera) return MensajeNoDisponible(action);
                    return null;

                case FriendRequestAction.DeleteFromHistory:
                    // Solo el emisor, solo aceptadas o rechazadas, y que no estén ya ocultas
                    if (solicitud.EmisorId != userId) return SinPermiso;
                    if (solicitud.Estado is not (EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada) || solicitud.OcultaParaEmisor)
                        return MensajeNoDisponible(action);
                    return null;

                default:
                    return SinPermiso;
            }
        }

        // Igual que ValidarParaAccion, pero además comprueba que ambas cuentas sigan activas
        private async Task<string?> ValidarConCuentasAsync(FriendRequest? solicitud, string userId, FriendRequestAction action)
        {
            if (solicitud is null)
                return MensajeNoDisponible(action);

            var error = ValidarParaAccion(solicitud, userId, action);
            if (error is not null)
                return error;

            if (!await _userQueryRepository.IsActiveAsync(solicitud.EmisorId) ||
                !await _userQueryRepository.IsActiveAsync(solicitud.ReceptorId))
                return MensajeInactivo(action);

            return null;
        }

        private static string MensajeNoDisponible(FriendRequestAction action) => action switch
        {
            FriendRequestAction.Accept => "Esta solicitud ya no se encuentra disponible para ser aceptada.",
            FriendRequestAction.Reject => "Esta solicitud ya no se encuentra disponible para ser rechazada.",
            FriendRequestAction.Cancel => "Esta solicitud ya no se encuentra disponible para ser cancelada.",
            _ => "Esta solicitud ya no se encuentra disponible para ser eliminada del historial."
        };

        private static string MensajeInactivo(FriendRequestAction action) => action switch
        {
            FriendRequestAction.Accept => "No se puede aceptar la solicitud porque uno de los usuarios se encuentra inactivo.",
            FriendRequestAction.Reject => "No se puede rechazar la solicitud porque uno de los usuarios se encuentra inactivo.",
            FriendRequestAction.Cancel => "No se puede cancelar la solicitud porque uno de los usuarios se encuentra inactivo.",
            _ => "No se puede realizar la acción porque uno de los usuarios se encuentra inactivo."
        };

        // ───────────────────────────── Auxiliares ─────────────────────────────

        private async Task<List<FriendRequestDto>> ToDtosAsync(
            string userId, List<FriendRequest> solicitudes, Func<FriendRequest, string> otroUsuarioId)
        {
            var usuarios = (await _userQueryRepository.GetActiveByIdsAsync(solicitudes.Select(otroUsuarioId)))
                .ToDictionary(u => u.Id);

            var resultado = new List<FriendRequestDto>();
            foreach (var solicitud in solicitudes)
            {
                if (!usuarios.TryGetValue(otroUsuarioId(solicitud), out var otroUsuario))
                    continue; // cuenta inactiva: se conserva la solicitud, pero no se muestra

                resultado.Add(new FriendRequestDto
                {
                    Id = solicitud.Id,
                    Estado = solicitud.Estado,
                    FechaEnvio = solicitud.FechaEnvio,
                    FechaRespuesta = solicitud.FechaRespuesta,
                    OtroUsuario = otroUsuario,
                    AmigosEnComun = await _friendshipService.CountMutualFriendsAsync(userId, otroUsuario.Id)
                });
            }
            return resultado;
        }
    }
}
