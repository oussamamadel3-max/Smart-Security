using Domain.Interfaces;

namespace Domain.Entities.Devices;

/// <summary>
/// سجل جهاز الأمان في الإدارة (لا يمثّل الاتصال اللحظي).
/// </summary>
public class Device : IAuditableEntity
{
    /// <summary>
    /// المعرف الداخلي للجهاز في الخادم (uuid).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// المعرف الثابت الذي يقدمه العتاد؛ فريد وغير فارغ، وليس سراً للمصادقة.
    /// </summary>
    public string HardwareUid { get; set; } = string.Empty;

    /// <summary>
    /// الاسم الظاهر في واجهات المالك والإدارة لتمييز الجهاز.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// حالة دورة حياة الإدارة: REGISTERED أو ACTIVE أو RETIRED (ليست حالة اتصال أو حماية).
    /// </summary>
    public string LifecycleStatus { get; set; } = string.Empty;

    /// <summary>
    /// آخر إصدار ملكية مُخصَّص للجهاز؛ يبدأ من 0 ويزداد عند الربط/النقل/إلغاء الربط، ويُفرض ≥ 0.
    /// </summary>
    public long CurrentOwnershipVersion { get; set; }

    /// <summary>
    /// حساب الأدمن الذي سجّل الجهاز في النظام.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// وقت تسجيل الجهاز بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// وقت آخر تعديل إداري على سجل الجهاز بتوقيت UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// وقت إخراج الجهاز من الإدارة؛ null يعني أنه غير متقاعد. يُملأ إلزامياً عند RETIRED.
    /// </summary>
    public DateTimeOffset? RetiredAt { get; set; }
}
