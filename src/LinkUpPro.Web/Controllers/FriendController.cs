using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.FriendRequests;
using LinkUpPro.Application.ViewModels.Friends;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class FriendController : Controller
    {
        private readonly IFriendshipService _friendshipService;
        private readonly IMapper _mapper;

        public FriendController(IFriendshipService friendshipService, IMapper mapper)
        {
            _friendshipService = friendshipService;
            _mapper = mapper;
        }

        // El Id del usuario sale siempre de la sesión
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ───────────────────────────── Pantalla principal ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var resumen = await _friendshipService.GetSummaryAsync(UserId);
            var amigos = await _friendshipService.GetFriendsAsync(UserId, search);

            return View(new FriendsIndexViewModel
            {
                TotalAmigos = resumen.TotalAmigos,
                PublicacionesDisponibles = resumen.PublicacionesDisponibles,
                Search = search?.Trim(),
                Amigos = _mapper.Map<List<FriendViewModel>>(amigos)
            });
        }

        // Reutiliza la vista de Solicitudes: sus enlaces "Volver atrás" apuntan a la pantalla actual
        [HttpGet]
        public async Task<IActionResult> MutualFriends(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == UserId)
                return RedirectToAction(nameof(Index));

            var amigos = await _friendshipService.GetMutualFriendsAsync(UserId, id);
            return View("~/Views/FriendRequest/MutualFriends.cshtml",
                new MutualFriendsViewModel { Amigos = _mapper.Map<List<UserSummaryViewModel>>(amigos) });
        }

        // ───────────────────────────── Eliminar amigo ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Remove(string id)
        {
            var resultado = await _friendshipService.GetFriendForRemovalAsync(UserId, id);
            if (!resultado.Succeeded)
            {
                TempData["Error"] = resultado.FirstError;
                return RedirectToAction(nameof(Index));
            }

            return View(new RemoveFriendViewModel { FriendId = id, UserName = resultado.Data!.UserName });
        }

        [HttpPost, ActionName(nameof(Remove))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveConfirmed(string id)
        {
            var resultado = await _friendshipService.RemoveFriendAsync(UserId, id);

            if (resultado.Succeeded)
                TempData["Success"] = "La amistad fue eliminada correctamente.";
            else
                TempData["Error"] = resultado.FirstError;

            return RedirectToAction(nameof(Index));
        }
    }
}
