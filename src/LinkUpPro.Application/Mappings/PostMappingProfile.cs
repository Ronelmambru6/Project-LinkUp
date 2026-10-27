using AutoMapper;
using LinkUpPro.Application.Dtos.Posts;
using LinkUpPro.Application.ViewModels.Friends;
using LinkUpPro.Application.ViewModels.Posts;

namespace LinkUpPro.Application.Mappings
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<PostDto, PostViewModel>()
                .ForMember(d => d.EsAutor, o => o.Ignore()) // lo calcula el controlador
                .ForMember(d => d.PuedeVerPerfilAutor, o => o.Ignore());

            // Formulario de edición: carga los datos actuales; la imagen nunca se precarga
            CreateMap<PostDto, SavePostViewModel>()
                .ForMember(d => d.Imagen, o => o.Ignore())
                .ForMember(d => d.RutaImagenActual, o => o.MapFrom(s => s.RutaImagen));

            CreateMap<SavePostViewModel, SavePostRequestDto>();
            CreateMap<PostFilterViewModel, PostFilterDto>();
            CreateMap<FriendPostFilterViewModel, FriendPostFilterDto>();
        }
    }
}