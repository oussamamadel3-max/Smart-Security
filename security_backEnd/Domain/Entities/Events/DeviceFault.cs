namespace Domain.Entities.Events;

/// <summary>
/// عطل مشخص أبلغ عنه الجهاز؛ لا يُنشأ لمجرد انقطاع النبضات.
/// </summary>
public class DeviceFault
{
    /// <summary>
    /// معرف صف العطل في الخادم.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز المعني بالعطل.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// المعرف الثابت للعطل من الجهاز؛ فريد مع الجهاز.
    /// </summary>
    public Guid FaultUid { get; set; }

    /// <summary>
    /// الملكية وقت العطل إن عُرفت؛ null خلاف ذلك. FK مركّب مع DeviceId.
    /// </summary>
    public Guid? OwnershipId { get; set; }

    /// <summary>
    /// الحساس المرتبط إن وُجد؛ null مسموح مع قيد مركّب عند التعبئة.
    /// </summary>
    public Guid? SensorId { get; set; }

    /// <summary>
    /// رمز التشخيص المحدد (مثل عطل قراءة مثبت).
    /// </summary>
    public string FaultCode { get; set; } = string.Empty;

    /// <summary>
    /// حالة العطل: ACTIVE أو RESOLVED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// وقت حدوث العطل وفق الجهاز إن كانت الساعة موثوقة؛ null إن غير متاح/غير موثوق.
    /// </summary>
    public DateTimeOffset? DeviceOccurredAt { get; set; }

    /// <summary>
    /// وقت استلام تقرير العطل في الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// وقت إثبات زوال العطل؛ null وهو ما زال ACTIVE.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// تفاصيل تشخيصية محدودة ومنزوعة الأسرار كـ JSON؛ null إن لا تفاصيل.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// إصدار الملكية الذي حمله تقرير العطل؛ 0 قبل أول ربط، للنسبة التاريخية عند التأخر.
    /// </summary>
    public long OwnershipVersion { get; set; }
}
