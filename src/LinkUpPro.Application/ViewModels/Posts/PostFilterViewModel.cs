using System.ComponentModel.DataAnnotations;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.ViewModels.Posts
{
    public class PostFilterViewModel : IValidatableObject
    {
        [Display(Name = "Texto de búsqueda")]
        public string? Texto { get; set; }

        [Display(Name = "Tipo de contenido")]
        public TipoContenido? TipoContenido { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha desde")]
        public DateTime? FechaDesde { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha hasta")]
        public DateTime? FechaHasta { get; set; }

        [Display(Name = "Estado de edición")]
        public FiltroEdicion Edicion { get; set; } = FiltroEdicion.Todas;

        public bool HayFiltros =>
            !string.IsNullOrWhiteSpace(Texto) || TipoContenido.HasValue ||
            FechaDesde.HasValue || FechaHasta.HasValue || Edicion != FiltroEdicion.Todas;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FechaDesde.HasValue && FechaHasta.HasValue && FechaDesde.Value.Date > FechaHasta.Value.Date)
                yield return new ValidationResult("La fecha inicial no puede ser posterior a la fecha final.");
        }
    }
}