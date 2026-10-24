using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Dtos.Friends
{
    public class FriendDto
    {
        public UserSummaryDto Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
    }
}
