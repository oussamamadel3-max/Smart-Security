namespace Domain.Exceptions;

/// <summary>
/// Base type for domain/business rule failures mapped by API error middleware.
/// </summary>
public abstract class BusinessException : Exception
{
    public string ErrorCode { get; }

    protected BusinessException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
