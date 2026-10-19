using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.Common;

namespace LinkUpPro.Application.ViewModels.Account
{
    public class ResetPasswordViewModel
    {
        // Llegan en el enlace del correo y viajan como campos ocultos del formulario
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [Compare(nameof(NewPassword), ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
