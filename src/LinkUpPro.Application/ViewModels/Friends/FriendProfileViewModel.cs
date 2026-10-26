using LinkUpPro.Application.ViewModels.Posts;
using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.ViewModels.Friends
{
    public class FriendProfileViewModel
    {
        public UserSummaryViewModel Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
        public List<PostViewModel> Publicaciones { get; set; } = new();
    }
}
