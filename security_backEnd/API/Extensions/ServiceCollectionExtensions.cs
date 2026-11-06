using Application.Interfaces.Services;
using Infrastructure;

namespace API.Extensions;

/// <summary>
/// Wires Application + Infrastructure registrations into the host.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAndInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.ConfigureInfrastructureServices(configuration);
        return services;
    }
}
