namespace Domain.Entities.Notifications;

/// <summary>
/// تنبيه يدوي من أدمن إلى مستخدم؛ يبقى للمراجعة حتى عند فشل FCM.
/// </summary>
public class AdminAlert
{
    /// <summary>
    /// معرف التنبيه.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الأدمن المرسل المخوّل.
    /// </summary>
    public Guid SenderUserId { get; set; }

    /// <summary>
    /// المستلم المستهدف.
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// عنوان التنبيه؛ غير فارغ.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// محتوى التنبيه؛ غير فارغ مع حد طول في API.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// وقت الحفظ قبل إرسال FCM بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
