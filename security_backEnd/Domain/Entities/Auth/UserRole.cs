using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Auth;

/// <summary>
/// ربط مستخدم بدور — المفتاح المركب (UserId, RoleId) من IdentityUserRole.
/// </summary>
public class UserRole : IdentityUserRole<Guid>
{
    /// <summary>
    /// وقت منح الدور بتوقيت UTC.
    /// </summary>
    public DateTimeOffset AssignedAt { get; set; }

    /// <summary>
    /// من منح الدور؛ null يعني بذر النظام أو عملية بلا ممثل بشري موثّق.
    /// </summary>
    public Guid? AssignedByUserId { get; set; }
}
