namespace Domain.Exceptions;

/// <summary>
/// Thrown for invalid client request shape / parameters.
/// </summary>
public sealed class BadRequestException : BusinessException
{
    public BadRequestException(string message, string errorCode = "BAD_REQUEST")
        : base(message, errorCode)
    {
    }
}
