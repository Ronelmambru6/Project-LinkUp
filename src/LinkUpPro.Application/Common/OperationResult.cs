namespace LinkUpPro.Application.Common
{
    // Resultado de una operación de negocio: éxito o un mensaje en español para el usuario.
    public class OperationResult
    {
        public bool Succeeded { get; private init; }
        public string? Message { get; private init; }

        public static OperationResult Ok() => new() { Succeeded = true };
        public static OperationResult Fail(string message) => new() { Message = message };
    }

    public class OperationResult<T>
    {
        public bool Succeeded { get; private init; }
        public string? Message { get; private init; }
        public T? Data { get; private init; }

        public static OperationResult<T> Ok(T data) => new() { Succeeded = true, Data = data };
        public static OperationResult<T> Fail(string message) => new() { Message = message };
    }
}