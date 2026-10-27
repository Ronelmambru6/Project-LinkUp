using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Dtos.Friends
{
    public class FriendProfileDto
    {
        public UserSummaryDto Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
        public List<PostDto> Publicaciones { get; set; } = new();
    }
}
