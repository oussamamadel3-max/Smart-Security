namespace Domain.Entities.Events;

/// <summary>
/// حادثة إنذار أنشأها الجهاز؛ التفرد على (DeviceId, EventUid) يمنع التكرار عند إعادة الإرسال.
/// </summary>
public class AlarmEvent
{
    /// <summary>
    /// معرف الحادثة في الخادم.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز الذي وقعت عليه الحادثة.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// المعرف الثابت الذي أنشأه الجهاز للحادثة.
    /// </summary>
    public Guid EventUid { get; set; }

    /// <summary>
    /// الملكية التاريخية عند الوقوع؛ null إن لم تُعرف. FK مركّب مع DeviceId عند الوجود.
    /// </summary>
    public Guid? OwnershipId { get; set; }

    /// <summary>
    /// إصدار الملكية الذي حمله الحدث؛ قد يكون 0 قبل أول ربط.
    /// </summary>
    public long OwnershipVersion { get; set; }

    /// <summary>
    /// الحساس المرتبط إن عُرف؛ null مسموح. FK مركّب يمنع حساساً لجهاز آخر.
    /// </summary>
    public Guid? SensorId { get; set; }

    /// <summary>
    /// لقطة معرف الحساس المحلي وقت الحادثة حتى بعد تغيير التهيئة.
    /// </summary>
    public string SensorKeySnapshot { get; set; } = string.Empty;

    /// <summary>
    /// حالة الحادثة: ACTIVE أو ENDED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// وقت وقوع الإنذار وفق ساعة الجهاز؛ لا يُعتمد إلا إذا ClockTrusted=true، وإلا null أو للعرض بحذر.
    /// </summary>
    public DateTimeOffset? OccurredDeviceAt { get; set; }

    /// <summary>
    /// وقت أول حفظ ناجح في الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// وقت عودة الحساس لوضعه الطبيعي وفق الجهاز؛ null إن لم تُبلَّغ النهاية بعد.
    /// </summary>
    public DateTimeOffset? EndedDeviceAt { get; set; }

    /// <summary>
    /// وقت استلام رسالة نهاية الحادثة في الخادم؛ null قبل الاستلام.
    /// </summary>
    public DateTimeOffset? EndedReceivedAt { get; set; }

    /// <summary>
    /// وقت تأكيد كتم صفارة هذه الحادثة؛ null إن لم تُكتم.
    /// </summary>
    public DateTimeOffset? MutedAt { get; set; }

    /// <summary>
    /// صلاحية ساعة الجهاز عند وقوع الحادثة.
    /// </summary>
    public bool ClockTrusted { get; set; }

    /// <summary>
    /// قياس مختصر وفق نوع الحساس كـ JSON؛ null إن لا ملخص. لا يخزن تاريخ النبضات كاملاً.
    /// </summary>
    public string? SignalSummary { get; set; }

    /// <summary>
    /// true إذا استُعيد الحدث من ذاكرة الجهاز بعد انقطاع؛ الافتراضي false.
    /// </summary>
    public bool WasBuffered { get; set; }
}
