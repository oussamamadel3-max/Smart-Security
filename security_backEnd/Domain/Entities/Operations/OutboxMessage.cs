namespace Domain.Entities.Operations;

/// <summary>
/// رسالة outbox تُدرج مع العملية في نفس المعاملة وتُنشر بعد الالتزام.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// معرف رسالة outbox.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// نوع المصدر التجميعي مثل DEVICE_COMMAND.
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// معرف المصدر المخزَّن (مثل معرف الأمر) للتحقق قبل النشر.
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// نوع رسالة العامل مثل PUBLISH_DEVICE_COMMAND.
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// حمولة منزوعة الأسرار ككائن JSON (معرفات ونسخ وتسلسل).
    /// </summary>
    public string Payload { get; set; } = "{}";

    /// <summary>
    /// وقت إدراج الصف مع العملية الأصلية بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// أول موعد يجوز فيه للعامل معالجة الرسالة بتوقيت UTC.
    /// </summary>
    public DateTimeOffset AvailableAt { get; set; }

    /// <summary>
    /// وقت إتمام محاولة النشر وفق العقد؛ null يعني غير مُعالَج بعد.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// عدد محاولات المعالجة؛ يبدأ من 0 ولا يُسمح أن يكون سالباً.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// رمز آخر إخفاق؛ null إن لا إخفاق مسجّل.
    /// </summary>
    public string? LastErrorCode { get; set; }
}
