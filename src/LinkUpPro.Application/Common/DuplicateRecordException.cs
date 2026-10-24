namespace LinkUpPro.Application.Common
{
    // Se lanza cuando un índice único de la base de datos rechaza un registro duplicado
    // (por ejemplo, dos clics seguidos en "Iniciar partida" o dos ataques a la misma celda).
    public class DuplicateRecordException : Exception
    {
        public DuplicateRecordException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}