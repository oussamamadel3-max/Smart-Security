namespace Domain.Entities.Commands;

/// <summary>
/// أمر تحكم موجَّه لجهاز؛ نجاح الإدراج لا يعني التنفيذ على العتاد.
/// </summary>
public class DeviceCommand
{
    /// <summary>
    /// معرف الأمر؛ يُرسل للجهاز كمفتاح إلغاء تكرار.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز المستهدف بالأمر.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// صف الملكية ACTIVE عند قبول الطلب؛ يُربَط بمركب (DeviceId, OwnershipId).
    /// </summary>
    public Guid OwnershipId { get; set; }

    /// <summary>
    /// نسخة الملكية المرتبطة بالأمر وقت الإنشاء.
    /// </summary>
    public long OwnershipVersion { get; set; }

    /// <summary>
    /// المستخدم الذي طلب الأمر (المالك الفعال).
    /// </summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>
    /// ترتيب متزايد لكل جهاز لمنع تنفيذ أمر أقدم بعد أحدث.
    /// </summary>
    public long CommandSequence { get; set; }

    /// <summary>
    /// نوع الأمر: ARM أو DISARM أو MUTE_ALARM أو TEST_SIREN أو STOP_TEST.
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// حادثة إنذار مرتبطة اختيارياً؛ null إن لا صف محفوظ بعد أو الأمر لا يخص حادثة. FK مركّب مع DeviceId.
    /// </summary>
    public Guid? AlarmEventId { get; set; }

    /// <summary>
    /// حالة الأمر: PENDING أو EXECUTED أو REJECTED أو TIMED_OUT أو CANCELLED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// وقت إنشاء الأمر بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// وقت انتهاء صلاحية الأمر بتوقيت UTC؛ يجب أن يكون لاحقاً لـ CreatedAt.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// وقت نشر رسالة الأمر؛ null إن لم تُرسل بعد.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// وقت ورود تأكيد الجهاز؛ null إن لم يُؤكَّد.
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>
    /// رمز رفض الجهاز أو الخادم بدون أسرار؛ null إن لم يُرفض.
    /// </summary>
    public string? RejectCode { get; set; }

    /// <summary>
    /// مفتاح إلغاء تكرار من العميل؛ null إن لم يُرسل. فريد مع طالب الأمر عند وجوده.
    /// </summary>
    public Guid? IdempotencyKey { get; set; }

    /// <summary>
    /// تسلسل الحالة الفعلية التي أكدها الجهاز؛ null قبل التأكيد.
    /// </summary>
    public long? ResultStateSequence { get; set; }

    /// <summary>
    /// معرف الحادثة الجارية المطلوب كتمها؛ إلزامي لـ MUTE_ALARM، وإلا null.
    /// </summary>
    public Guid? TargetAlarmEventUid { get; set; }

    /// <summary>
    /// تسلسل الحالة المؤكدة عند قبول الأمر؛ يُستخدم بعد TIMED_OUT لإثبات تقرير أحدث.
    /// </summary>
    public long ObservedStateSequence { get; set; }

    /// <summary>
    /// وقت مصالحة أمر منتهِ المهلة بتقرير أحدث أو تأكيد موثوق؛ null إن لم تُصالح. لا يعني وحده نجاح التنفيذ.
    /// </summary>
    public DateTimeOffset? ReconciledAt { get; set; }
}
