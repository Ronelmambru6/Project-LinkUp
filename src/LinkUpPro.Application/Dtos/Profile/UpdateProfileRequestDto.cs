using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Application.Dtos.Profile
{
    // Solo los campos que se pueden modificar. Usuario, correo, estado, etc. no están aquí a propósito:
    // aunque alguien los agregue al formulario, nunca llegan al servicio.
    public class UpdateProfileRequestDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public IFormFile? NuevaFoto { get; set; }

        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
