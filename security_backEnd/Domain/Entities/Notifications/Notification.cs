namespace Domain.Entities.Notifications;

/// <summary>
/// طلب إشعار لمستلم واحد ومصدر واحد؛ قبول المزود لا يثبت وصول الهاتف.
/// </summary>
public class Notification
{
    /// <summary>
    /// معرف طلب الإشعار.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// المستلم المقصود.
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// نوع الإشعار: ALARM أو FAULT أو ADMIN_ALERT.
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// مصدر حادثة الإنذار؛ يُملأ فقط عندما النوع ALARM، وإلا null.
    /// </summary>
    public Guid? AlarmEventId { get; set; }

    /// <summary>
    /// مصدر العطل؛ يُملأ فقط عندما النوع FAULT، وإلا null.
    /// </summary>
    public Guid? DeviceFaultId { get; set; }

    /// <summary>
    /// مصدر التنبيه الإداري؛ يُملأ فقط عندما النوع ADMIN_ALERT، وإلا null.
    /// </summary>
    public Guid? AdminAlertId { get; set; }

    /// <summary>
    /// حالة الطلب: QUEUED أو SENDING أو ACCEPTED أو FAILED أو CANCELLED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// وقت حفظ الطلب بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// وقت آخر محاولة إرسال؛ null قبل أي محاولة.
    /// </summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>
    /// موعد إعادة المحاولة التالي عند خطأ مؤقت؛ null إن لا جدولة.
    /// </summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// وقت إلغاء الطلب (نقل ملكية/إبطال مستلم)؛ null إن لم يُلغَ. لا يُستخدم FAILED لإخفاء الإلغاء.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// سبب الإلغاء؛ null إن لم يُلغَ.
    /// </summary>
    public string? CancellationReason { get; set; }
}
