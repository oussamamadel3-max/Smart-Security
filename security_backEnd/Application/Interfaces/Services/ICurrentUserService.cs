namespace Application.Interfaces.Services;

/// <summary>
/// Exposes the authenticated user identity for the current request.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
