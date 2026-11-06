namespace Domain.Entities.Events;

/// <summary>
/// علامة فقد أحداث محلية (مثل امتلاء المخزن)؛ لا تعني معرفة كل الأحداث المفقودة.
/// </summary>
public class EventLossReport
{
    /// <summary>
    /// معرف صف التقرير في الخادم.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز الذي أبلغ عن الفقد.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// المعرف الثابت للتقرير من الجهاز؛ فريد مع الجهاز.
    /// </summary>
    public Guid ReportUid { get; set; }

    /// <summary>
    /// الملكية وقت العلامة إن عُرفت؛ null خلاف ذلك.
    /// </summary>
    public Guid? OwnershipId { get; set; }

    /// <summary>
    /// سبب العلامة؛ يبدأ بـ BUFFER_FULL في النسخة الأولى.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// عدد الأحداث المفقودة إن استطاع الجهاز قياسه (≥ 0)؛ null إن غير مثبت.
    /// </summary>
    public int? LostCount { get; set; }

    /// <summary>
    /// وقت الرصد وفق الجهاز إن توفر؛ null إن غير متاح.
    /// </summary>
    public DateTimeOffset? DeviceOccurredAt { get; set; }

    /// <summary>
    /// وقت استلام العلامة في الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }

    /// <summary>
    /// إصدار الملكية عند علامة الفقد؛ 0 قبل أول ربط.
    /// </summary>
    public long OwnershipVersion { get; set; }
}
