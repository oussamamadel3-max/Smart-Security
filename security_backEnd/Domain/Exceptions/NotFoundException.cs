namespace Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found.
/// </summary>
public sealed class NotFoundException : BusinessException
{
    public NotFoundException(string message = "العنصر غير موجود.", string errorCode = "NOT_FOUND")
        : base(message, errorCode)
    {
    }
}
