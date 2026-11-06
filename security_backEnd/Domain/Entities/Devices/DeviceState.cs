namespace Domain.Entities.Devices;

/// <summary>
/// آخر حالة مؤكدة وصلت من الجهاز (صف واحد لكل جهاز).
/// </summary>
public class DeviceState
{
    /// <summary>
    /// معرف الجهاز؛ مفتاح أساسي ومفتاح خارجي.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// آخر نبضة أو حالة صالحة بتوقيت UTC؛ null إن لم يُر الجهاز بعد. الاتصال يُحسب مع مهلة الإعدادات.
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>
    /// وقت استلام آخر تقرير حالة صالح بتوقيت UTC؛ null إن لم يُستلم تقرير حالة.
    /// </summary>
    public DateTimeOffset? LastStateAt { get; set; }

    /// <summary>
    /// آخر تسلسل حالة قبله الخادم؛ يبدأ من 0 ولا يُسمح له بالتراجع، وحدة عداد لا تُعاد زمنياً.
    /// </summary>
    public long StateSequence { get; set; }

    /// <summary>
    /// حالة التسليح المؤكدة: ARMED أو DISARMED أو UNKNOWN.
    /// </summary>
    public string ArmState { get; set; } = string.Empty;

    /// <summary>
    /// حالة الصفارة المؤكدة: OFF أو ALARM أو TEST أو UNKNOWN.
    /// </summary>
    public string SirenState { get; set; } = string.Empty;

    /// <summary>
    /// الحالة الصحية العامة: HEALTHY أو FAULT أو UNKNOWN.
    /// </summary>
    public string HealthState { get; set; } = string.Empty;

    /// <summary>
    /// رمز سبب العطل المقاس إن وُجد؛ null عند غياب قياس مثبت.
    /// </summary>
    public string? HealthCode { get; set; }

    /// <summary>
    /// مصدر القدرة المبلَّغ: MAINS أو BATTERY أو UNKNOWN؛ null عندما لا تدعم الدائرة القياس.
    /// </summary>
    public string? PowerSource { get; set; }

    /// <summary>
    /// نسبة البطارية من 0 إلى 100؛ null إذا كان القياس غير متاح.
    /// </summary>
    public short? BatteryPercent { get; set; }

    /// <summary>
    /// آخر إصدار ملكية أقره الجهاز؛ يبدأ من 0 قبل أي إقرار.
    /// </summary>
    public long ConfirmedOwnershipVersion { get; set; }

    /// <summary>
    /// آخر إصدار تهيئة أقره الجهاز؛ يبدأ من 0 قبل أي إقرار.
    /// </summary>
    public long AppliedSettingsVersion { get; set; }

    /// <summary>
    /// نسخة البرنامج الثابت كما أبلغ الجهاز؛ null إن لم تُبلَّغ.
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// وقت الجهاز المبلَّغ إن وُجد؛ لا يُعتمد إلا مع ClockTrusted=true.
    /// </summary>
    public DateTimeOffset? DeviceReportedAt { get; set; }

    /// <summary>
    /// هل يمكن اعتماد ساعة الجهاز؛ الافتراضي false.
    /// </summary>
    public bool ClockTrusted { get; set; }

    /// <summary>
    /// معرف الحادثة الجارية على الجهاز؛ يبقى بعد كتم الصوت ويُفرَّغ فقط عند انتهاء الحادثة؛ null إن لا حادثة جارية.
    /// </summary>
    public Guid? CurrentAlarmEventUid { get; set; }
}
