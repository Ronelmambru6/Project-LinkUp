using AutoMapper;
using LinkUpPro.Application.Dtos.Account;
using LinkUpPro.Application.ViewModels.Account;

namespace LinkUpPro.Application.Mappings
{
    public class AccountMappingProfile : Profile
    {
        public AccountMappingProfile()
        {
            CreateMap<SaveRegisterViewModel, RegisterRequestDto>();
        }
    }
}
