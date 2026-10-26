using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.Friends
{
    public class FriendViewModel
    {
        public UserSummaryViewModel Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
    }
}
