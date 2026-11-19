namespace Application.Options;

/// <summary>
/// FCM delivery worker and push-token crypto options.
/// Real Firebase send stays behind <see cref="Provider"/> = Firebase and explicit ops enablement.
/// Credentials use Application Default Credentials / GOOGLE_APPLICATION_CREDENTIALS — never appsettings secrets.
/// </summary>
public sealed class FcmOptions
{
    public const string SectionName = "Fcm";

    public const string ProviderFake = "Fake";
    public const string ProviderFirebase = "Firebase";

    /// <summary>
    /// When false, the hosted worker idles; <c>INotificationDispatchService</c> remains callable for tests.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Delivery provider name: Fake (default) or Firebase.
    /// Firebase must not be selected in automated tests.
    /// </summary>
    public string Provider { get; set; } = ProviderFake;

    /// <summary>
    /// Expected Firebase GCP project id when <see cref="Provider"/> is Firebase (e.g. smart-security-dev-28642).
    /// Compared to the service-account JSON <c>project_id</c> to prevent cross-environment credentials.
    /// Leave empty with Fake; required for Firebase registration.
    /// </summary>
    public string FirebaseProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Poll interval for the hosted worker when enabled.
    /// </summary>
    public int PollIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Max notification rows claimed per dispatch cycle.
    /// </summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Delay before retrying after a RETRYABLE provider result.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Soft ceiling on provider attempts per notification before parking as FAILED
    /// (only after a documented provider attempt sets <c>last_attempt_at</c>).
    /// </summary>
    public int MaxAttempts { get; set; } = 8;

    /// <summary>
    /// Base64-encoded 32-byte AES key for push-token ciphertext (User Secrets / env).
    /// Not a Firebase credential.
    /// </summary>
    public string TokenEncryptionKey { get; set; } = string.Empty;
}
