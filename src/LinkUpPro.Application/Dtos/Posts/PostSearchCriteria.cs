using LinkUpPro.Domain.Enums;

namespace LinkUpPro.Application.Dtos.Posts
{
    // Criterios ya listos para consultar la base de datos: fechas en UTC.
    public class PostSearchCriteria
    {
        public string AutorId { get; set; } = string.Empty;
        public string? Texto { get; set; }
        public TipoContenido? TipoContenido { get; set; }
        public DateTime? DesdeUtc { get; set; }

        // Límite superior EXCLUSIVO: el inicio del día siguiente a "fecha hasta"
        public DateTime? HastaUtcExclusivo { get; set; }
        public FiltroEdicion Edicion { get; set; } = FiltroEdicion.Todas;
    }
}