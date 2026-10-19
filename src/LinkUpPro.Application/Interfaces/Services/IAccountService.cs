using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.Dtos.Common;

namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IAccountService
    {
        // activationUrl / resetUrl: dirección de la acción correspondiente; el servicio le agrega userId y token.
        Task<ServiceResult> RegisterAsync(RegisterRequestDto request, string activationUrl);

        Task<ConfirmEmailStatus> ConfirmEmailAsync(string? userId, string? token);

        Task<ResendActivationStatus> ResendActivationAsync(string userName, string activationUrl);

        Task<LoginStatus> LoginAsync(LoginRequestDto request);

        Task LogoutAsync();

        Task ForgotPasswordAsync(string userName, string resetUrl);

        Task<ResetPasswordStatus> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
