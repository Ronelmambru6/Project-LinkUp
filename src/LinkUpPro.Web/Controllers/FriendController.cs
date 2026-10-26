using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.FriendRequests;
using LinkUpPro.Application.ViewModels.Friends;
using LinkUpPro.Application.ViewModels.Posts;
using LinkUpPro.Application.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    [Authorize]
    public class FriendController : Controller
    {
        private readonly IFriendshipService _friendshipService;
        private readonly IFriendFeedService _friendFeedService;
        private readonly IMapper _mapper;

        public FriendController(IFriendshipService friendshipService, IFriendFeedService friendFeedService, IMapper mapper)
        {
            _friendshipService = friendshipService;
            _friendFeedService = friendFeedService;
            _mapper = mapper;
        }

        // El Id del usuario sale siempre de la sesión
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ───────────────────────────── Pantalla principal ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(string? search, [FromQuery] FriendPostFilterViewModel filtros)
        {
            // Fechas mal escritas, valores manipulados o rango invertido: se muestra el error y la lista completa
            var errores = new List<string>();
            foreach (var (clave, entrada) in ModelState)
            {
                foreach (var error in entrada.Errors)
                {
                    var mensaje = clave switch
                    {
                        "" => error.ErrorMessage, // validación de rango de fechas del ViewModel
                        nameof(FriendPostFilterViewModel.FechaDesde) or nameof(FriendPostFilterViewModel.FechaHasta) => "La fecha ingresada no es válida.",
                        _ => "El filtro seleccionado no es válido."
                    };
                    if (!string.IsNullOrWhiteSpace(mensaje) && !errores.Contains(mensaje))
                        errores.Add(mensaje);
                }
            }
            ModelState.Clear();

            var resumen = await _friendshipService.GetSummaryAsync(UserId);
            var amigos = await _friendshipService.GetFriendsAsync(UserId, search);
            var amigosFiltro = await _friendFeedService.GetFriendsForFilterAsync(UserId);

            // Publicaciones: con filtros si son válidos; si el servicio los rechaza (amigo que ya no lo es,
            // fechas invertidas), se muestra su mensaje y la lista sin filtrar
            var aplicarFiltros = errores.Count == 0 && filtros.HayFiltrosAmigos;
            var resultado = await _friendFeedService.GetFriendsPostsAsync(UserId,
                aplicarFiltros ? _mapper.Map<FriendPostFilterDto>(filtros) : new FriendPostFilterDto());

            if (!resultado.Succeeded)
            {
                errores.Add(resultado.FirstError);
                aplicarFiltros = false;
                resultado = await _friendFeedService.GetFriendsPostsAsync(UserId, new FriendPostFilterDto());
            }

            return View(new FriendsIndexViewModel
            {
                TotalAmigos = resumen.TotalAmigos,
                PublicacionesDisponibles = resumen.PublicacionesDisponibles,
                Filtros = filtros,
                AmigosFiltro = _mapper.Map<List<UserSummaryViewModel>>(amigosFiltro),
                ErroresFiltro = errores,
                Publicaciones = APublicacionesDeAmigos(resultado.Data),
                BusquedaConFiltros = aplicarFiltros,
                Search = search?.Trim(),
                Amigos = _mapper.Map<List<FriendViewModel>>(amigos)
            });
        }

        // ───────────────────────────── Perfil del amigo ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Profile(string id)
        {
            var resultado = await _friendFeedService.GetFriendProfileAsync(UserId, id);
            if (!resultado.Succeeded || resultado.Data is null)
            {
                TempData["Error"] = resultado.FirstError;
                return RedirectToAction(nameof(Index));
            }

            return View(new FriendProfileViewModel
            {
                Usuario = _mapper.Map<UserSummaryViewModel>(resultado.Data.Usuario),
                AmigosEnComun = resultado.Data.AmigosEnComun,
                Publicaciones = APublicacionesDeAmigos(resultado.Data.Publicaciones)
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

        // ───────────────────────────── Auxiliares ─────────────────────────────

        // Son publicaciones de amigos: nunca del usuario (sin Editar ni Eliminar) y el autor enlaza a su perfil
        private List<PostViewModel> APublicacionesDeAmigos(List<PostDto>? publicaciones)
        {
            var vms = _mapper.Map<List<PostViewModel>>(publicaciones ?? new List<PostDto>());
            foreach (var vm in vms)
            {
                vm.EsAutor = false;
                vm.PuedeVerPerfilAutor = true;
            }
            return vms;
        }
    }
}
