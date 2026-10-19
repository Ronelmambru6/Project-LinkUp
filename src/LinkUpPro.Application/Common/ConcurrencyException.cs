namespace LinkUpPro.Application.Common
{
    // Se lanza cuando otra operación modificó el mismo registro al mismo tiempo.
    // La capa de persistencia traduce la excepción de EF a esta, para que Application no dependa de EF.
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message, Exception? innerException = null)
            : base(message, innerException) { }
    }
}