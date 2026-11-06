using System.Security.Claims;
using Application.Interfaces.Services;
using Domain.Exceptions;

namespace API.Middleware;

/// <summary>
/// After JWT authentication, re-validates IsActive/DeletedAt and auth_sessions on every protected request.
/// </summary>
public sealed class ActiveSessionMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService auth)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.User.FindFirstValue("sub");
            var sid = context.User.FindFirstValue("sid");

            if (!Guid.TryParse(sub, out var userId) || !Guid.TryParse(sid, out var sessionId))
                throw new UnauthenticatedUserException();

            await auth.EnsureUserAndSessionActiveAsync(userId, sessionId, context.RequestAborted);
        }

        await _next(context);
    }
}

public static class ActiveSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseActiveSessionValidation(this IApplicationBuilder app)
        => app.UseMiddleware<ActiveSessionMiddleware>();
}
