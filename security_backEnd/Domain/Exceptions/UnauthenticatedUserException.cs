namespace Domain.Exceptions;

/// <summary>
/// Thrown when the caller is not authenticated.
/// </summary>
public sealed class UnauthenticatedUserException : BusinessException
{
    public UnauthenticatedUserException(string message = "غير مصرح.")
        : base(message, "AUTH_UNAUTHENTICATED")
    {
    }
}
