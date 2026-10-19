namespace LinkUpPro.Application.Dtos.Common
{
    // Resultado de una operación de servicio. Field indica a qué campo del formulario
    // pertenece el error ("" = error general del formulario).
    public class ServiceResult
    {
        public bool Succeeded { get; init; }
        public List<ServiceError> Errors { get; init; } = new();

        public static ServiceResult Success() => new() { Succeeded = true };

        public static ServiceResult Failure(string field, string message) =>
            new() { Succeeded = false, Errors = { new ServiceError(field, message) } };
    }

    public record ServiceError(string Field, string Message);
}
