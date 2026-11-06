namespace Domain.Entities.Commands;

/// <summary>
/// سجل إلحاقي لانتقالات حالة الأمر؛ لا يُعدَّل بعد الإدراج.
/// </summary>
public class CommandStatusHistory
{
    /// <summary>
    /// معرف متسلسل GENERATED ALWAYS AS IDENTITY.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// الأمر الذي انتقلت حالته.
    /// </summary>
    public Guid CommandId { get; set; }

    /// <summary>
    /// الحالة السابقة؛ null عند إنشاء الأمر أول مرة.
    /// </summary>
    public string? FromStatus { get; set; }

    /// <summary>
    /// الحالة الجديدة بعد الانتقال.
    /// </summary>
    public string ToStatus { get; set; } = string.Empty;

    /// <summary>
    /// سبب الرفض أو المهلة أو الإلغاء؛ null إن لا رمز سبب.
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// وقت تسجيل الانتقال على الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// true إذا وصل التأكيد بعد انتهاء مهلة الانتظار.
    /// </summary>
    public bool IsLateAck { get; set; }
}
