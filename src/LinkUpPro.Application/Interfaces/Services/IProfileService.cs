using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.Profile;

namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetAsync(string userId);

        // Data = true si también se cambió la contraseña (hay que cerrar la sesión)
        Task<ServiceResult<bool>> UpdateAsync(string userId, UpdateProfileRequestDto request);
    }
}
