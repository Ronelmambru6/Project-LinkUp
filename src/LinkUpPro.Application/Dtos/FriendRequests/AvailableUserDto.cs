using LinkUpPro.Application.Dtos.Users;

namespace LinkUpPro.Application.Dtos.FriendRequests
{
    // Usuario al que se le puede enviar una solicitud de amistad
    public class AvailableUserDto
    {
        public UserSummaryDto Usuario { get; set; } = new();
        public int AmigosEnComun { get; set; }
    }
}
