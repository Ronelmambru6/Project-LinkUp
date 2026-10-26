using AutoMapper;
using LinkUpPro.Application.Dtos.Friends;
using LinkUpPro.Application.ViewModels.Friends;

namespace LinkUpPro.Application.Mappings
{
    public class FriendMappingProfile : Profile
    {
        public FriendMappingProfile()
        {
            // UserSummaryDto → UserSummaryViewModel ya está en FriendRequestMappingProfile
            CreateMap<FriendDto, FriendViewModel>();
        }
    }
}
