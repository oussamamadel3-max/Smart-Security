using Application.Dtos.FcmDtos;
using Application.Interfaces.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using AppFcmOptions = Application.Options.FcmOptions;

namespace Infrastructure.Services.Fcm;

/// <summary>
/// Real FCM HTTP v1 via Firebase Admin SDK.
/// Uses Application Default Credentials / GOOGLE_APPLICATION_CREDENTIALS only.
/// Must not be registered unless <c>Fcm:Provider=Firebase</c> is explicitly configured.
/// Never logs credential file contents or raw device tokens.
/// </summary>
public sealed class FirebaseFcmProvider : IFcmProvider
{
    private readonly ILogger<FirebaseFcmProvider> _logger;
    private readonly Lazy<FirebaseMessaging> _messaging;

    public FirebaseFcmProvider(ILogger<FirebaseFcmProvider> logger)
    {
        _logger = logger;
        _messaging = new Lazy<FirebaseMessaging>(CreateMessaging, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string ProviderName => AppFcmOptions.ProviderFirebase;

    public async Task<FcmSendResult> SendAsync(FcmSendRequest request, CancellationToken ct = default)
    {
        var message = new Message
        {
            Token = request.RawToken,
            Data = request.Payload.Data.ToDictionary(static kv => kv.Key, static kv => kv.Value),
            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        try
        {
            var messageId = await _messaging.Value.SendAsync(message, ct).ConfigureAwait(false);
            return FcmSendResult.Accepted(messageId);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(
                "Firebase messaging failed for notification {NotificationId} token {PushTokenId}: {ErrorCode}",
                request.NotificationId,
                request.PushTokenId,
                ex.MessagingErrorCode?.ToString() ?? ex.ErrorCode.ToString());

            return MapFirebaseError(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Unexpected Firebase send failure for notification {NotificationId}.",
                request.NotificationId);
            return FcmSendResult.Retryable("FIREBASE_UNEXPECTED");
        }
    }

    private static FcmSendResult MapFirebaseError(FirebaseMessagingException ex)
    {
        var code = ex.MessagingErrorCode;
        return code switch
        {
            MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch
                => FcmSendResult.Failed(code.ToString()!, revokeToken: true),
            MessagingErrorCode.InvalidArgument
                => FcmSendResult.Failed(code.ToString()!, revokeToken: false),
            MessagingErrorCode.QuotaExceeded
                or MessagingErrorCode.Unavailable
                or MessagingErrorCode.Internal
                => FcmSendResult.Retryable(code.ToString()!),
            _ => FcmSendResult.Retryable(ex.ErrorCode.ToString())
        };
    }

    private FirebaseMessaging CreateMessaging()
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            // ADC resolves GOOGLE_APPLICATION_CREDENTIALS or metadata server — never embed JSON.
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault()
            });
            _logger.LogInformation(
                "FirebaseApp initialized via Application Default Credentials for FCM (no credential material logged).");
        }

        return FirebaseMessaging.DefaultInstance;
    }
}
