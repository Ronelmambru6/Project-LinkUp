using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Application.ViewModels.Account
{
    public class ResendActivationViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; } = string.Empty;
    }
}
