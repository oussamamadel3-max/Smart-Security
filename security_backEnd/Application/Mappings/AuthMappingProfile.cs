using Application.Dtos.AuthDtos;
using Domain.Entities.Auth;
using Mapster;

namespace Application.Mappings;

/// <summary>
/// Mapster registrations for auth-related DTOs.
/// </summary>
public sealed class AuthMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, MeResponseDto>();
    }
}
