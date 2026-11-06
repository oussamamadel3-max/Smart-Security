namespace Application.Dtos.FcmDtos;

/// <summary>
/// Minimal data-only FCM payload (identifiers only — no secrets or narrative body).
/// </summary>
public sealed class FcmDataPayload
{
    public required IReadOnlyDictionary<string, string> Data { get; init; }
}

public sealed class FcmSendRequest
{
    public required Guid NotificationId { get; init; }
    public required Guid PushTokenId { get; init; }
    public required string RawToken { get; init; }
    public required FcmDataPayload Payload { get; init; }
}

/// <summary>
/// Provider outcome. ACCEPTED means the provider accepted the request — not device receipt or user read.
/// </summary>
public sealed class FcmSendResult
{
    public required string ProviderResult { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? ErrorCode { get; init; }

    /// <summary>
    /// When true, the worker must revoke the push token (permanent invalid registration).
    /// </summary>
    public bool RevokeToken { get; init; }

    public static FcmSendResult Accepted(string? messageId)
        => new()
        {
            ProviderResult = Domain.Constants.DbStatusCodes.ProviderResult.Accepted,
            ProviderMessageId = messageId,
            ErrorCode = null,
            RevokeToken = false
        };

    public static FcmSendResult Retryable(string errorCode)
        => new()
        {
            ProviderResult = Domain.Constants.DbStatusCodes.ProviderResult.Retryable,
            ProviderMessageId = null,
            ErrorCode = errorCode,
            RevokeToken = false
        };

    public static FcmSendResult Failed(string errorCode, bool revokeToken = false)
        => new()
        {
            ProviderResult = Domain.Constants.DbStatusCodes.ProviderResult.Failed,
            ProviderMessageId = null,
            ErrorCode = errorCode,
            RevokeToken = revokeToken
        };
}
