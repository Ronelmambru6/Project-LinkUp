using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    public class AvailableUserViewModel
    {
        public UserSummaryViewModel Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
    }
}
