namespace Domain.Entities.Notifications;

/// <summary>
/// رمز استقبال FCM لهاتف/متصفح مرتبط بحساب؛ الرمز الخام مشفّر خارج مفتاح قاعدة البيانات.
/// </summary>
public class PushToken
{
    /// <summary>
    /// معرف صف الرمز.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// صاحب الرمز.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// رمز FCM مشفّر بمفتاح خارج قاعدة البيانات (bytea).
    /// </summary>
    public byte[] TokenCiphertext { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// بصمة حتمية للبحث عن التكرار؛ فريدة عالمياً.
    /// </summary>
    public byte[] TokenFingerprint { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// منصة الاستقبال: ANDROID أو IOS أو WEB.
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// وقت تسجيل الرمز بتوقيت UTC.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// آخر استخدام ناجح/محاولة مرتبطة بالرمز؛ null إن لم يُستخدم بعد.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// وقت إبطال الرمز؛ null يعني أنه ما زال مرشحاً للإرسال.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
