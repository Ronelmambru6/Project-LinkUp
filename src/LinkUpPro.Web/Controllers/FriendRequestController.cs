using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.FriendRequests;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.FriendRequests;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class FriendRequestController : Controller
    {
        private readonly IFriendRequestService _friendRequestService;
        private readonly IFriendshipService _friendshipService;
        private readonly IMapper _mapper;

        public FriendRequestController(IFriendRequestService friendRequestService, IFriendshipService friendshipService, IMapper mapper)
        {
            _friendRequestService = friendRequestService;
            _friendshipService = friendshipService;
            _mapper = mapper;
        }

        // El Id del usuario sale siempre de la sesión, nunca del formulario ni de la URL
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ───────────────────────────── Listados ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var modelo = new FriendRequestsIndexViewModel
            {
                Pendientes = _mapper.Map<List<FriendRequestViewModel>>(await _friendRequestService.GetPendingReceivedAsync(UserId)),
                Enviadas = _mapper.Map<List<FriendRequestViewModel>>(await _friendRequestService.GetSentAsync(UserId))
            };
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> MutualFriends(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == UserId)
                return RedirectToAction(nameof(Index));

            var amigos = await _friendshipService.GetMutualFriendsAsync(UserId, id);
            return View(new MutualFriendsViewModel { Amigos = _mapper.Map<List<UserSummaryViewModel>>(amigos) });
        }

        // ───────────────────────────── Nueva solicitud ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> New(string? search)
        {
            var modelo = new SaveFriendRequestViewModel { Search = search?.Trim() };
            await CargarUsuariosDisponiblesAsync(modelo);
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(SaveFriendRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _friendRequestService.SendAsync(UserId, model.SelectedUserId);
                if (resultado.Succeeded)
                {
                    TempData["Success"] = "La solicitud de amistad fue enviada correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(error.Field, error.Message);
            }

            await CargarUsuariosDisponiblesAsync(model);
            return View(nameof(New), model);
        }

        // ───────────────────────────── Acciones con confirmación ─────────────────────────────
        // GET muestra la pantalla de confirmación; POST ejecuta la acción.

        [HttpGet]
        public Task<IActionResult> Accept(int id) => MostrarConfirmacionAsync(id, FriendRequestAction.Accept);

        [HttpPost, ActionName(nameof(Accept))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptConfirmed(int id) =>
            Resultado(await _friendRequestService.AcceptAsync(id, UserId), "La solicitud fue aceptada correctamente.");

        [HttpGet]
        public Task<IActionResult> Reject(int id) => MostrarConfirmacionAsync(id, FriendRequestAction.Reject);

        [HttpPost, ActionName(nameof(Reject))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectConfirmed(int id) =>
            Resultado(await _friendRequestService.RejectAsync(id, UserId), "La solicitud fue rechazada correctamente.");

        [HttpGet]
        public Task<IActionResult> Cancel(int id) => MostrarConfirmacionAsync(id, FriendRequestAction.Cancel);

        [HttpPost, ActionName(nameof(Cancel))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id) =>
            Resultado(await _friendRequestService.CancelAsync(id, UserId), "La solicitud fue cancelada correctamente.");

        [HttpGet]
        public Task<IActionResult> DeleteFromHistory(int id) => MostrarConfirmacionAsync(id, FriendRequestAction.DeleteFromHistory);

        [HttpPost, ActionName(nameof(DeleteFromHistory))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromHistoryConfirmed(int id) =>
            Resultado(await _friendRequestService.DeleteFromHistoryAsync(id, UserId), "La solicitud fue eliminada de su historial.");

        // ───────────────────────────── Auxiliares ─────────────────────────────

        private async Task<IActionResult> MostrarConfirmacionAsync(int id, FriendRequestAction accion)
        {
            var resultado = await _friendRequestService.GetForConfirmationAsync(id, UserId, accion);
            if (!resultado.Succeeded)
            {
                TempData["Error"] = resultado.FirstError;
                return RedirectToAction(nameof(Index));
            }

            return View("Confirm", new FriendRequestConfirmViewModel
            {
                Id = id,
                Accion = accion,
                UserName = resultado.Data!.OtroUsuario.UserName
            });
        }

        private IActionResult Resultado(ServiceResult resultado, string mensajeExito)
        {
            if (resultado.Succeeded)
                TempData["Success"] = mensajeExito;
            else
                TempData["Error"] = resultado.FirstError;

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarUsuariosDisponiblesAsync(SaveFriendRequestViewModel modelo)
        {
            var usuarios = await _friendRequestService.GetAvailableUsersAsync(UserId, modelo.Search);
            modelo.Usuarios = _mapper.Map<List<AvailableUserViewModel>>(usuarios);
        }
    }
}
