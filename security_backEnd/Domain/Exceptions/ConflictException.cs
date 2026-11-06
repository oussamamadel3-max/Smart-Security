namespace Domain.Exceptions;

/// <summary>
/// Thrown when a business conflict prevents the requested state change.
/// </summary>
public sealed class ConflictException : BusinessException
{
    public ConflictException(string message, string errorCode = "CONFLICT")
        : base(message, errorCode)
    {
    }
}
