using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Auth;

/// <summary>
/// حساب المستخدم — الهوية الوحيدة للنظام عبر ASP.NET Core Identity (بدون جدول مستخدمين موازٍ).
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// اسم العرض الظاهر في الواجهات والسجلات؛ إلزامي وغير فارغ بعد trim (قيد قاعدة + تحقق تطبيقي).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// هل الحساب مسموح له بعمليات جديدة؛ عند التعطيل أو الأرشفة يُضبط إلى false فوراً.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// وقت إنشاء الحساب في الخادم بتوقيت UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// وقت آخر تعديل إداري على الحساب بتوقيت UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// وقت الأرشفة المنطقية؛ null يعني أن الحساب غير مؤرشف. عند التعبئة يجب أن يكون IsActive=false.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
