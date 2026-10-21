namespace LinkUpPro.Application.Dtos.Common
{
    // Resultado de una operación de servicio. Field indica a qué campo del formulario
    // pertenece el error ("" = error general del formulario).
    public class ServiceResult
    {
        public bool Succeeded { get; init; }
        public List<ServiceError> Errors { get; init; } = new();

        public string FirstError => Errors.FirstOrDefault()?.Message ?? string.Empty;

        public static ServiceResult Success() => new() { Succeeded = true };

        public static ServiceResult Failure(string message) => Failure(string.Empty, message);

        public static ServiceResult Failure(string field, string message) =>
            new() { Succeeded = false, Errors = { new ServiceError(field, message) } };
    }

    // Igual que ServiceResult, pero además devuelve un dato cuando la operación sale bien.
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };

        public static new ServiceResult<T> Failure(string message) =>
            new() { Succeeded = false, Errors = { new ServiceError(string.Empty, message) } };
    }

    public record ServiceError(string Field, string Message);
}
