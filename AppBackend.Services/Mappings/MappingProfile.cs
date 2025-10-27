using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AutoMapper;

namespace AppBackend.Services.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region User
            // Map RegisterRequest -> User
            // Ignore PasswordHash because it will be set after hashing
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Map CreateUserRequest -> User (Admin creates user)
            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Map User -> UserDto for responses (legacy)
            CreateMap<User, UserDto>();

            // Map User -> UserResponseDto (detailed response with role)
            CreateMap<User, UserResponseDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : null));
            #endregion

            #region Account
            // Add mappings for Account entities here later
            #endregion

            #region Order
            // Add mappings for Order entities here later
            #endregion
        }
    }
}