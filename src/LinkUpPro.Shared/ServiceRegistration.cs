using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Las implementaciones de IEmailService e IImageStorageService se registran en la issue #6
            return services;
        }
    }
}