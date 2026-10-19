using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUpPro.Persistence.Identity
{
    // Tokens de restablecimiento de contraseña: válidos 1 hora.
    public class PasswordResetTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<PasswordResetTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }

        public bool IsExpired(string token) => TokenExpiration.IsExpired(Protector, Options.TokenLifespan, token);
    }

    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public const string ProviderName = "PasswordReset";

        public PasswordResetTokenProviderOptions()
        {
            Name = "PasswordResetDataProtectorTokenProvider";
        }
    }
}
