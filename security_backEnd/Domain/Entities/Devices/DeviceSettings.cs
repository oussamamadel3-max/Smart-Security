namespace Domain.Entities.Devices;

/// <summary>
/// التهيئة التنفيذية المعتمدة المرغوبة للجهاز (1:1 مع devices).
/// </summary>
public class DeviceSettings
{
    /// <summary>
    /// معرف الجهاز؛ مفتاح أساسي ومفتاح خارجي في آن واحد.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// إصدار التهيئة المرغوبة؛ موجب ومتزايد، ويختلف عن الإصدار المُقر من الجهاز.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// شرط تحقق الإنذار والمعايرة ككائن JSON؛ لا تُزرع قيم إنتاجية تخمينية لـ SW-420 قبل الاختبار.
    /// </summary>
    public string AlarmRule { get; set; } = "{}";

    /// <summary>
    /// مدة اختبار الصفارة بالمللي ثانية؛ القرار المعتمد للاختبار 30000 (30 ثانية).
    /// </summary>
    public int TestDurationMs { get; set; }

    /// <summary>
    /// مهلة اعتبار الجهاز غير متصل بالثواني منذ آخر نبضة/حالة صالحة؛ القرار المعتمد 3600.
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; }

    /// <summary>
    /// مهلة انتظار تأكيد الأمر بالثواني؛ القرار المعتمد 10، ولا تعني انتهاء صلاحية التنفيذ.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// سعة مخزن أحداث الانقطاع (عدد أحداث موجب). القيمة الرقمية الإنتاجية معلقة على القياس؛ بلا DEFAULT إنتاجي.
    /// </summary>
    public int EventBufferCapacity { get; set; }

    /// <summary>
    /// مدة صلاحية تنفيذ الأمر بالثواني (موجبة). القيمة الرقمية الإنتاجية معلقة على الاختبار؛ بلا DEFAULT إنتاجي.
    /// </summary>
    public int CommandTtlSeconds { get; set; }

    /// <summary>
    /// وقت آخر تعديل على التهيئة بتوقيت UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// المستخدم الذي اعتمد آخر تعديل على التهيئة.
    /// </summary>
    public Guid UpdatedByUserId { get; set; }
}
