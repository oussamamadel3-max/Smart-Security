using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Auth;

/// <summary>
/// دور تفويض أساسي (OWNER / ADMIN) عبر IdentityRole دون جدول أدوار موازٍ.
/// رمز الدور يُخزَّن في <see cref="IdentityRole{TKey}.Name"/> ويُسمَّى عمودياً <c>code</c>.
/// </summary>
public class Role : IdentityRole<Guid>
{
    /// <summary>
    /// اسم العرض للدور في الواجهات الإدارية؛ مستقل عن رمز التفويض الثابت.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
