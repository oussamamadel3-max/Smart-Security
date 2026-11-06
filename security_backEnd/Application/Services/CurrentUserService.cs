using Application.Interfaces.Services;
using Application.Services.AuthServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

/// <summary>
/// Reads the current user from HttpContext claims.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public string? UserId => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirstValue("sub");

    public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
