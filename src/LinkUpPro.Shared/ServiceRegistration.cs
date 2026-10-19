using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Shared.Services;
using LinkUpPro.Shared.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Shared
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSharedLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

            services.AddTransient<IEmailService, EmailService>();
            services.AddScoped<IImageStorageService, ImageStorageService>();

            return services;
        }
    }
}
