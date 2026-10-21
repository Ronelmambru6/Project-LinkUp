using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Profile;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Persistence.Identity
{
    // Vive en Persistence porque modifica ApplicationUser mediante UserManager (Identity).
    public class ProfileService : IProfileService
    {
        private const string CarpetaFotos = "profiles";
        private const long TamanoMaximoFoto = 5 * 1024 * 1024;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageStorageService _imageStorage;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(UserManager<ApplicationUser> userManager, IImageStorageService imageStorage, ILogger<ProfileService> logger)
        {
            _userManager = userManager;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        public async Task<ProfileDto?> GetAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return null;

            return new ProfileDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Telefono = user.PhoneNumber ?? string.Empty,
                FotoPerfil = user.FotoPerfil
            };
        }

        public async Task<ServiceResult<bool>> UpdateAsync(string userId, UpdateProfileRequestDto request)
        {
            // El Id viene de la sesión: un usuario solo puede modificar su propio perfil
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || !user.EmailConfirmed)
                return ServiceResult<bool>.Failure("No posee permisos para modificar este perfil.");

            var cambiaContrasena = !string.IsNullOrEmpty(request.NewPassword);

            // 1. Contraseña actual: se comprueba ANTES de guardar la foto, para no dejar archivos sueltos
            if (cambiaContrasena && !await _userManager.CheckPasswordAsync(user, request.CurrentPassword ?? string.Empty))
                return Fallo("CurrentPassword", "La contraseña actual es incorrecta.");

            // 2. Nueva foto (opcional): se valida y se guarda, pero la anterior todavía no se borra
            string? rutaNuevaFoto = null;
            if (request.NuevaFoto is not null)
            {
                if (request.NuevaFoto.Length > TamanoMaximoFoto)
                    return Fallo("NuevaFoto", "La imagen seleccionada no puede superar los 5 MB.");

                if (!_imageStorage.IsValidImage(request.NuevaFoto))
                    return Fallo("NuevaFoto", "El archivo seleccionado no tiene un formato de imagen válido.");

                try
                {
                    rutaNuevaFoto = await _imageStorage.SaveAsync(request.NuevaFoto, CarpetaFotos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo guardar la nueva foto de perfil del usuario {UserId}.", user.Id);
                    return Fallo("NuevaFoto", "No fue posible actualizar la foto de perfil. Inténtelo nuevamente.");
                }
            }

            // 3. Datos personales. Solo se tocan estos campos: usuario, correo, estado, fecha de
            //    registro, amistades, publicaciones y partidas nunca se modifican desde aquí.
            var fotoAnterior = user.FotoPerfil;
            user.Nombre = request.Nombre.Trim();
            user.Apellido = request.Apellido.Trim();
            user.PhoneNumber = request.Telefono.Trim();
            user.FechaModificacion = DateTime.UtcNow;
            if (rutaNuevaFoto is not null)
                user.FotoPerfil = rutaNuevaFoto;

            // 4. Guardado en UNA sola operación: ChangePasswordAsync guarda la contraseña nueva junto con
            //    los datos personales. Si la contraseña falla, no se guarda nada (sin cambios parciales).
            //    Además cambia el sello de seguridad, lo que invalida todas las sesiones anteriores.
            var resultado = cambiaContrasena
                ? await _userManager.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword!)
                : await _userManager.UpdateAsync(user);

            if (!resultado.Succeeded)
            {
                // No se guardó el perfil: la foto nueva sobra y la anterior se conserva
                if (rutaNuevaFoto is not null)
                    await _imageStorage.DeleteAsync(rutaNuevaFoto);

                return resultado.Errors.Any(e => e.Code == "PasswordMismatch")
                    ? Fallo("CurrentPassword", "La contraseña actual es incorrecta.")
                    : resultado.Errors.Any(e => e.Code.StartsWith("Password"))
                        ? Fallo("NewPassword", "La nueva contraseña no cumple las reglas de seguridad.")
                        : Fallo(string.Empty, "No fue posible actualizar el perfil. Inténtelo nuevamente.");
            }

            // 5. Solo ahora, con el perfil ya guardado, se borra la foto anterior
            if (rutaNuevaFoto is not null && !string.IsNullOrEmpty(fotoAnterior))
                await _imageStorage.DeleteAsync(fotoAnterior);

            return ServiceResult<bool>.Success(cambiaContrasena);
        }

        private static ServiceResult<bool> Fallo(string campo, string mensaje) =>
            new() { Succeeded = false, Errors = { new ServiceError(campo, mensaje) } };
    }
}
