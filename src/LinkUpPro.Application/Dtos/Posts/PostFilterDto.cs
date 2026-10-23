using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Posts
{
    // Filtros del Home tal como los escribe el usuario (fechas en su hora local).
    public class PostFilterDto
    {
        public string? Texto { get; set; }
        public TipoContenido? TipoContenido { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public FiltroEdicion Edicion { get; set; } = FiltroEdicion.Todas;
    }
}