using AutoMapper;
using LinkUpPro.Application.Dtos.FriendRequests;
using LinkUpPro.Application.Dtos.Users;
using LinkUpPro.Application.ViewModels.FriendRequests;
using LinkUpPro.Application.ViewModels.Users;

namespace LinkUpPro.Application.Mappings
{
    public class FriendRequestMappingProfile : Profile
    {
        public FriendRequestMappingProfile()
        {
            CreateMap<UserSummaryDto, UserSummaryViewModel>();
            CreateMap<FriendRequestDto, FriendRequestViewModel>();
            CreateMap<AvailableUserDto, AvailableUserViewModel>();
        }
    }
}
