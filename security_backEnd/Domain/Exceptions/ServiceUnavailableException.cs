namespace Domain.Exceptions;

/// <summary>
/// Thrown when a required external dependency (e.g. MQTT broker ACL revoke) cannot complete.
/// </summary>
public sealed class ServiceUnavailableException : BusinessException
{
    public ServiceUnavailableException(string message, string errorCode = "SERVICE_UNAVAILABLE")
        : base(message, errorCode)
    {
    }
}
