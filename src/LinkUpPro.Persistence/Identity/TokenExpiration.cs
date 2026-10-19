using Microsoft.AspNetCore.DataProtection;

namespace LinkUpPro.Persistence.Identity
{
    // El enunciado pide un mensaje distinto para un enlace "vencido" y uno "inválido o usado".
    // Identity solo responde "no válido", así que aquí se lee la fecha de creación guardada en el token.
    internal static class TokenExpiration
    {
        public static bool IsExpired(IDataProtector protector, TimeSpan lifespan, string token)
        {
            try
            {
                var datos = protector.Unprotect(Convert.FromBase64String(token));
                using var stream = new MemoryStream(datos);
                using var reader = new BinaryReader(stream);
                var fechaCreacion = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
                return fechaCreacion + lifespan < DateTimeOffset.UtcNow;
            }
            catch
            {
                return false; // token manipulado o ilegible: se trata como inválido
            }
        }
    }
}
