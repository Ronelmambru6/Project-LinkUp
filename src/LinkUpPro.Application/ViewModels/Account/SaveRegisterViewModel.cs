using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.Common;
using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Application.ViewModels.Account
{
    // Nota: [Required] ya rechaza los textos que solo tienen espacios.
    public class SaveRegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^(809|829|849)-\d{3}-\d{4}$",
            ErrorMessage = "El teléfono debe tener un formato válido de República Dominicana, por ejemplo 809-555-1234.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(256, ErrorMessage = "El correo electrónico no puede superar los 256 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La foto de perfil es obligatoria.")]
        [Display(Name = "Foto de perfil")]
        public IFormFile? FotoPerfil { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$",
            ErrorMessage = "El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
