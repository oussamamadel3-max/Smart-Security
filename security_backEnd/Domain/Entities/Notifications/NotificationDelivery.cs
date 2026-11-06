namespace Domain.Entities.Notifications;

/// <summary>
/// محاولة تسليم إشعار لرمز دفع محدد؛ لا تخزّن «قراءة المستخدم».
/// </summary>
public class NotificationDelivery
{
    /// <summary>
    /// معرف المحاولة المتسلسل GENERATED ALWAYS AS IDENTITY.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// طلب الإشعار الأب.
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// رمز الدفع المستهدف في هذه المحاولة.
    /// </summary>
    public Guid PushTokenId { get; set; }

    /// <summary>
    /// رقم المحاولة لهذا الإشعار والرمز؛ يبدأ من 1 وفريد معها.
    /// </summary>
    public short AttemptNo { get; set; }

    /// <summary>
    /// وقت استدعاء المزود بتوقيت UTC.
    /// </summary>
    public DateTimeOffset AttemptedAt { get; set; }

    /// <summary>
    /// نتيجة المزود: ACCEPTED أو RETRYABLE أو FAILED.
    /// </summary>
    public string ProviderResult { get; set; } = string.Empty;

    /// <summary>
    /// معرف رسالة FCM إن وُجد؛ null خلاف ذلك.
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// رمز فشل خالٍ من الأسرار؛ null عند القبول.
    /// </summary>
    public string? ErrorCode { get; set; }
}
