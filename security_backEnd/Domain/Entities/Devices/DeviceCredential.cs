namespace Domain.Entities.Devices;

/// <summary>
/// بيانات اعتماد مصادقة الجهاز لدى الوسيط/الخادم (بدون أسرار صريحة قابلة للاسترجاع في السجلات).
/// </summary>
public class DeviceCredential
{
    /// <summary>
    /// معرف صف الاعتماد.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// الجهاز المرتبط بهذا الاعتماد.
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// نوع الاعتماد: PASSWORD_HASH أو CERTIFICATE أو SECRET_REF.
    /// </summary>
    public string AuthType { get; set; } = string.Empty;

    /// <summary>
    /// قيمة تحقق مشتقة من سر الجهاز؛ تُملأ فقط عندما AuthType=PASSWORD_HASH، وإلا null.
    /// </summary>
    public string? Verifier { get; set; }

    /// <summary>
    /// بصمة الشهادة؛ تُملأ فقط عند CERTIFICATE، وإلا null.
    /// </summary>
    public string? CertificateFingerprint { get; set; }

    /// <summary>
    /// مرجع إلى مدير الأسرار؛ يُملأ فقط عند SECRET_REF، وإلا null.
    /// </summary>
    public string? SecretRef { get; set; }

    /// <summary>
    /// وقت إنشاء الاعتماد بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// وقت إبطال الاعتماد؛ null يعني أن الصف ما زال مرشحاً للمصادقة وفق السياسة.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
