using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Application
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Busca todos los perfiles de AutoMapper de esta capa
            services.AddAutoMapper(
                cfg => cfg.LicenseKey = configuration["AutoMapper:LicenseKey"],
                typeof(ServiceRegistration));

            services.AddScoped<IFriendshipService, FriendshipService>();
            services.AddScoped<IFriendRequestService, FriendRequestService>();
            services.AddScoped<IPostService, PostService>();

            // Aquí se irán registrando los servicios de cada módulo
            services.AddScoped<IBattleshipService, BattleshipService>();

            return services;
        }
    }
}