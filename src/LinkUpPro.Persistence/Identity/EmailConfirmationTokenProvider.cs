using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUpPro.Persistence.Identity
{
    // Tokens de activación de cuenta: válidos 24 horas.
    public class EmailConfirmationTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public EmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<EmailConfirmationTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }

        public bool IsExpired(string token) => TokenExpiration.IsExpired(Protector, Options.TokenLifespan, token);
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
