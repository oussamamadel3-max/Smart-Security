namespace Domain.Entities.Operations;

/// <summary>
/// سجل إداري إلحاقي؛ يُمنع تحديثه/حذفه ضمن صلاحيات التطبيق.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// معرف متسلسل GENERATED ALWAYS AS IDENTITY.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// منفّذ العملية؛ null للعملية النظامية بلا ممثل مستخدم.
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// رمز العمل مثل DEVICE_TRANSFERRED أو COMMAND_REQUESTED.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// نوع الهدف النصي (جهاز، مستخدم، أمر…).
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// معرف الهدف المحفوظ حتى بعد الأرشفة؛ null إن لا هدف محدد. ليس FK متعدد الأشكال.
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// الجهاز المرتبط إن كانت العملية تخص جهازاً؛ null خلاف ذلك.
    /// </summary>
    public Guid? DeviceId { get; set; }

    /// <summary>
    /// نتيجة التسجيل: SUCCESS أو REJECTED أو FAILED.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// وقت التسجيل في الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// معرفات وأسباب آمنة فقط كـ JSON؛ null إن لا تفاصيل. بلا أسرار أو رموز FCM.
    /// </summary>
    public string? Details { get; set; }
}
