namespace Domain.Entities.Devices;

/// <summary>
/// حساس مركَّب على جهاز؛ المعرف المحلي فريد داخل الجهاز الواحد.
/// </summary>
public class DeviceSensor
{
    /// <summary>
    /// معرف الحساس في الخادم.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز الذي رُكِّب عليه الحساس.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// المعرف المحلي للحساس داخل الجهاز (مستقر عبر التقارير).
    /// </summary>
    public string SensorKey { get; set; } = string.Empty;

    /// <summary>
    /// نوع الحساس الحقيقي: VIBRATION أو MOTION أو OTHER.
    /// </summary>
    public string SensorType { get; set; } = string.Empty;

    /// <summary>
    /// اسم العرض في الواجهات.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// هل الحساس مُدرج في التهيئة التشغيلية؛ لا يثبت سلامة الدائرة.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// حالة التشخيص: HEALTHY أو FAULT أو UNKNOWN (الافتراضي UNKNOWN عند غياب القياس).
    /// </summary>
    public string DiagnosticStatus { get; set; } = string.Empty;

    /// <summary>
    /// رمز عطل مرصود إن وُجد؛ null يعني عدم وجود رمز مثبت.
    /// </summary>
    public string? DiagnosticCode { get; set; }

    /// <summary>
    /// آخر إشارة استلمها الخادم من الحساس بتوقيت UTC؛ null إن لم تُستلم أي إشارة.
    /// </summary>
    public DateTimeOffset? LastSignalAt { get; set; }

    /// <summary>
    /// معايرة/تهيئة خاصة بالنوع ككائن JSON؛ null إن لا توجد معايرة مخزَّنة بعد.
    /// </summary>
    public string? Configuration { get; set; }
}
