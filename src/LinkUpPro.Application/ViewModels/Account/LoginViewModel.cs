using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Application.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Mantener sesión iniciada")]
        public bool RememberMe { get; set; }

        // Pantalla a la que volver después del login (solo direcciones internas)
        public string? ReturnUrl { get; set; }
    }
}
