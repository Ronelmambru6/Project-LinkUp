using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Persistence.Identity
{
    public static class AuthenticationSetup
    {
        // IMPORTANTE: llamar DESPUÉS de AddIdentity, porque reemplaza parte de su configuración de cookies.
        public static IServiceCollection AddLinkUpAuthentication(this IServiceCollection services)
        {
            // Para limitar el reenvío de activación a una solicitud cada 5 minutos
            services.AddMemoryCache();

            // Enunciado: cambiar o restablecer la contraseña invalida todas las sesiones anteriores.
            // Con intervalo cero, el sello de seguridad se comprueba en cada solicitud.
            services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/Login";

                // Enunciado: HttpOnly, Secure y SameSite
                options.Cookie.Name = "LinkUpPro.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;

                // Sin "Mantener sesión": 30 minutos de inactividad
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;

                options.Events.OnValidatePrincipal = async context =>
                {
                    // Se conserva la validación del sello de seguridad de Identity
                    await SecurityStampValidator.ValidatePrincipalAsync(context);

                    // Por defecto la cookie solo se renueva pasada la mitad del tiempo.
                    // Renovarla en cada solicitud hace que sean 30 minutos exactos sin actividad.
                    if (context.Principal is not null && context.Properties.IsPersistent == false)
                        context.ShouldRenew = true;
                };
            });

            return services;
        }
    }
}
