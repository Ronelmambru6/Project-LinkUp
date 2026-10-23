using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.ViewModels.Posts;
using LinkUpPro.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPostService _postService;
    private readonly IMapper _mapper;

    public HomeController(ILogger<HomeController> logger, IPostService postService, IMapper mapper)
    {
        _logger = logger;
        _postService = postService;
        _mapper = mapper;
    }

    // El Id sale siempre de la sesión, nunca del formulario
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PostFilterViewModel filtros)
    {
        // Fechas mal escritas o rango invertido: se muestra el error y la lista completa
        var errores = new List<string>();
        foreach (var entrada in ModelState)
        {
            foreach (var error in entrada.Value!.Errors)
            {
                var mensaje = entrada.Key is nameof(PostFilterViewModel.FechaDesde) or nameof(PostFilterViewModel.FechaHasta)
                    && !string.IsNullOrEmpty(error.ErrorMessage) && !error.ErrorMessage.StartsWith("La fecha")
                    ? "La fecha ingresada no es válida."
                    : error.ErrorMessage;
                if (!string.IsNullOrWhiteSpace(mensaje) && !errores.Contains(mensaje))
                    errores.Add(mensaje);
            }
        }
        ModelState.Clear();

        ViewBag.FiltroErrores = errores;
        var vm = await ConstruirAsync(new SavePostViewModel(), filtros, aplicarFiltros: errores.Count == 0);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavePostViewModel model)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _postService.CreateAsync(UserId, _mapper.Map<SavePostRequestDto>(model));
            if (resultado.Succeeded)
            {
                TempData["Success"] = "La publicación fue creada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
                ModelState.AddModelError(error.Field, error.Message);
        }

        // Se vuelve a mostrar el Home con el formulario y sus errores
        ViewBag.FiltroErrores = new List<string>();
        var vm = await ConstruirAsync(model, new PostFilterViewModel(), aplicarFiltros: false);
        return View(nameof(Index), vm);
    }

    private async Task<HomeIndexViewModel> ConstruirAsync(SavePostViewModel nueva, PostFilterViewModel filtros, bool aplicarFiltros)
    {
        var filtroAplicado = aplicarFiltros ? filtros : new PostFilterViewModel();
        var resultado = await _postService.GetMyPostsAsync(UserId, _mapper.Map<PostFilterDto>(filtroAplicado));

        var publicaciones = new List<PostViewModel>();
        if (resultado.Succeeded && resultado.Data is not null)
        {
            publicaciones = _mapper.Map<List<PostViewModel>>(resultado.Data);
            // Todas las publicaciones del Home son del usuario de la sesión
            foreach (var p in publicaciones)
                p.EsAutor = true;
        }
        else
        {
            TempData["Error"] = resultado.FirstError;
        }

        return new HomeIndexViewModel
        {
            Nueva = nueva,
            Filtros = filtros,
            Publicaciones = publicaciones,
            BusquedaConFiltros = aplicarFiltros && filtros.HayFiltros
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
