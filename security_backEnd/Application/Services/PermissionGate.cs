using System.Security.Claims;
using Application.Common.Permissions;
using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

/// <summary>
/// Deny-by-default permission gate based on authenticated role claims.
/// </summary>
public sealed class PermissionGate : IPermissionGate
{
    private readonly IHttpContextAccessor _http;

    public PermissionGate(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Task EnsureAll(params string[] permissionKeys)
    {
        if (permissionKeys is null || permissionKeys.Length == 0)
            throw new ForbiddenException();

        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthenticatedUserException();

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(user.FindAll("role").Select(c => c.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (!RolePermissionMap.HasAll(roles, permissionKeys))
            throw new ForbiddenException();

        return Task.CompletedTask;
    }
}
