using AutoMapper;
using LinkUpPro.Application.Dtos.Profile;
using LinkUpPro.Application.ViewModels.Profile;

namespace LinkUpPro.Application.Mappings
{
    public class ProfileMappingProfile : Profile
    {
        public ProfileMappingProfile()
        {
            // Las contraseñas nunca se cargan en el formulario
            CreateMap<ProfileDto, SaveProfileViewModel>()
                .ForMember(d => d.FotoActual, o => o.MapFrom(s => s.FotoPerfil))
                .ForMember(d => d.NuevaFoto, o => o.Ignore())
                .ForMember(d => d.CurrentPassword, o => o.Ignore())
                .ForMember(d => d.NewPassword, o => o.Ignore())
                .ForMember(d => d.ConfirmPassword, o => o.Ignore());

            CreateMap<SaveProfileViewModel, UpdateProfileRequestDto>();
        }
    }
}
