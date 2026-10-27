using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Web.Controllers
{
    // Público: el error puede ocurrir antes de iniciar sesión.
    // Sin [HttpGet] a propósito: si falla un POST, el error se vuelve a ejecutar como POST.
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error/{code:int?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index(int? code)
        {
            ViewData["Mensaje"] = code == 404
                ? "La página solicitada no se encuentra disponible."
                : "Ocurrió un error al procesar la solicitud. Inténtelo nuevamente.";

            return View("~/Views/Shared/Error.cshtml");
        }
    }
}