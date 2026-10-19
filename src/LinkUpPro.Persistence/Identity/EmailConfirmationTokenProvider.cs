using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUpPro.Persistence.Identity
{
    // Identity usa una sola duración para todos sus tokens. El enunciado pide 24 h para la
    // activación y 1 h para restablecer la contraseña, así que la activación usa este proveedor propio.
    public class EmailConfirmationTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public EmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<EmailConfirmationTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }

        // El enunciado pide un mensaje distinto para "vencido" y para "inválido o usado".
        // Identity solo dice "no válido", así que aquí se lee la fecha de creación del token.
        public bool IsExpired(string token)
        {
            try
            {
                var datos = Protector.Unprotect(Convert.FromBase64String(token));
                using var stream = new MemoryStream(datos);
                using var reader = new BinaryReader(stream);
                var fechaCreacion = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
                return fechaCreacion + Options.TokenLifespan < DateTimeOffset.UtcNow;
            }
            catch
            {
                return false; // token manipulado o ilegible: se trata como inválido
            }
        }
    }

    public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public const string ProviderName = "EmailConfirmation";

        public EmailConfirmationTokenProviderOptions()
        {
            Name = "EmailConfirmationDataProtectorTokenProvider";
        }
    }
}
