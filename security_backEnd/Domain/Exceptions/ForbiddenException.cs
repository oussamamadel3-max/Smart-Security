namespace Domain.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but lacks permission.
/// </summary>
public sealed class ForbiddenException : BusinessException
{
    public ForbiddenException(string message = "ليس لديك صلاحية لإجراء هذه العملية.")
        : base(message, "AUTH_FORBIDDEN")
    {
    }
}
