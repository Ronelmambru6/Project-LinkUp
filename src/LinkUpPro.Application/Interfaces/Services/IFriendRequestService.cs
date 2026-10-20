using LinkUpPro.Application.Dtos.Common;
using LinkUpPro.Application.Dtos.FriendRequests;

namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IFriendRequestService
    {
        Task<List<FriendRequestDto>> GetPendingReceivedAsync(string userId);
        Task<List<FriendRequestDto>> GetSentAsync(string userId);
        Task<int> CountPendingReceivedAsync(string userId);

        Task<List<AvailableUserDto>> GetAvailableUsersAsync(string userId, string? search);
        Task<ServiceResult> SendAsync(string senderId, string? receiverId);

        // Valida que la solicitud pueda usarse para la acción (permisos y estado) antes de mostrar la confirmación
        Task<ServiceResult<FriendRequestDto>> GetForConfirmationAsync(int requestId, string userId, FriendRequestAction action);

        Task<ServiceResult> AcceptAsync(int requestId, string userId);
        Task<ServiceResult> RejectAsync(int requestId, string userId);
        Task<ServiceResult> CancelAsync(int requestId, string userId);
        Task<ServiceResult> DeleteFromHistoryAsync(int requestId, string userId);
    }
}
