using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Application.Dtos.Account
{
    // Los nombres coinciden con RegisterViewModel para que los errores
    // del servicio se muestren junto al campo correcto del formulario.
    public class RegisterRequestDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IFormFile FotoPerfil { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
