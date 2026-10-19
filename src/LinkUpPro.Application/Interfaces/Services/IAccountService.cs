using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.Dtos.Common;

namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IAccountService
    {
        // activationUrl: dirección de la acción ConfirmEmail; el servicio le agrega userId y token.
        Task<ServiceResult> RegisterAsync(RegisterRequestDto request, string activationUrl);

        Task<ConfirmEmailStatus> ConfirmEmailAsync(string? userId, string? token);

        Task<LoginStatus> LoginAsync(LoginRequestDto request);

        Task LogoutAsync();

        Task<ResendActivationStatus> ResendActivationAsync(string userName, string activationUrl);
    }
}
