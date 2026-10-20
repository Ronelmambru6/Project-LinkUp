using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LinkUpPro.Application.ViewModels.Profile
{
    public class SaveProfileViewModel : IValidatableObject
    {
        // ── Solo lectura: se cargan desde la base de datos y nunca se toman del formulario ──
        [BindNever, ValidateNever]
        public string UserName { get; set; } = string.Empty;

        [BindNever, ValidateNever]
        public string Email { get; set; } = string.Empty;

        [BindNever, ValidateNever]
        public string FotoActual { get; set; } = string.Empty;

        // ── Datos personales ── ([Required] ya rechaza los textos que solo tienen espacios)
        [Required(ErrorMessage = "Debe ingresar su nombre.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar su apellido.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe ingresar un número telefónico válido de República Dominicana.")]
        [RegularExpression(@"^\s*(809|829|849)-\d{3}-\d{4}\s*$",
            ErrorMessage = "Debe ingresar un número telefónico válido de República Dominicana.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        // Opcional: si se deja vacío, se conserva la foto actual
        [Display(Name = "Nueva foto de perfil")]
        public IFormFile? NuevaFoto { get; set; }

        // ── Cambio de contraseña (opcional) ──
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string? ConfirmPassword { get; set; }

        public bool QuiereCambiarContrasena =>
            !string.IsNullOrEmpty(CurrentPassword) || !string.IsNullOrEmpty(NewPassword) || !string.IsNullOrEmpty(ConfirmPassword);

        // Reglas del cambio de contraseña con los mensajes exactos del enunciado.
        // La comprobación de que la contraseña actual es correcta la hace el servicio.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!QuiereCambiarContrasena)
                yield break; // los tres vacíos: se conserva la contraseña

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                yield return new ValidationResult(
                    "Para cambiar su contraseña debe completar la contraseña actual, la nueva contraseña y su confirmación.");
                yield break;
            }

            if (NewPassword != ConfirmPassword)
                yield return new ValidationResult("La nueva contraseña y su confirmación no coinciden.", new[] { nameof(ConfirmPassword) });

            var faltan = PasswordRules.GetMissingCriteria(NewPassword);
            if (faltan.Count > 0)
                yield return new ValidationResult(
                    $"La nueva contraseña no cumple las reglas de seguridad. Le falta: {string.Join(", ", faltan)}.",
                    new[] { nameof(NewPassword) });

            if (NewPassword == CurrentPassword)
                yield return new ValidationResult("La nueva contraseña debe ser diferente de la contraseña actual.", new[] { nameof(NewPassword) });
        }
    }
}
