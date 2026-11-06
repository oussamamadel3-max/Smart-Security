namespace Domain.Entities.Devices;

/// <summary>
/// فترة ملكية جهاز عبر الزمن؛ يُسمح بسجل مفتوح واحد فقط لكل جهاز.
/// </summary>
public class DeviceOwnership
{
    /// <summary>
    /// معرف صف الملكية (uuid).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز التابع لهذه الفترة.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// المالك المحدد لهذه الفترة.
    /// </summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>
    /// رقم إصدار الملكية على الجهاز؛ موجب وفريد لكل جهاز.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// حالة الفترة: PENDING أو ACTIVE أو ENDED أو CANCELLED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// وقت بدء فترة الملكية بتوقيت UTC (التزام النقل أو بعد إقرار الربط الأولي).
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// وقت تفعيل الملكية؛ null قبل الإقرار في الربط الأولي.
    /// </summary>
    public DateTimeOffset? ActivatedAt { get; set; }

    /// <summary>
    /// وقت إسقاط الوصول أو إلغاء الطلب؛ null يعني أن الفترة ما زالت مفتوحة.
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// الأدمن الذي أنشأ ربط/نقل الملكية.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// من أنهى أو ألغى الملكية؛ null إن لم تُنهَ بعد أو أُنهيت بلا ممثل موثّق.
    /// </summary>
    public Guid? EndedByUserId { get; set; }
}
