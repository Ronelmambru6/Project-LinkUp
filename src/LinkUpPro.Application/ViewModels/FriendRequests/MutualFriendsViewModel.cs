using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.FriendRequests
{
    public class MutualFriendsViewModel
    {
        public List<UserSummaryViewModel> Amigos { get; set; } = new();
    }
}
