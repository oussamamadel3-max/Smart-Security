using Domain.Constants;

namespace API.IntegrationTests.Support;

/// <summary>
/// معرفات ثابتة لإعادة إنشاء بيانات الاختبار دون تكرار غير مقصود.
/// </summary>
internal static class SeedIds
{
    public static readonly Guid AdminUser = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid OwnerA = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid OwnerB = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid ArchivedUser = Guid.Parse("11111111-1111-1111-1111-111111111104");

    public static readonly Guid RoleAdmin = SystemRoleIds.Admin;
    public static readonly Guid RoleOwner = SystemRoleIds.Owner;

    public static readonly Guid DeviceActive = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid DeviceRegistered = Guid.Parse("33333333-3333-3333-3333-333333333302");

    public static readonly Guid OwnershipEnded = Guid.Parse("44444444-4444-4444-4444-444444444401");
    public static readonly Guid OwnershipActive = Guid.Parse("44444444-4444-4444-4444-444444444402");

    public static readonly Guid SensorVib = Guid.Parse("55555555-5555-5555-5555-555555555501");
    public static readonly Guid Credential = Guid.Parse("55555555-5555-5555-5555-555555555502");

    public static readonly Guid AlarmActive = Guid.Parse("66666666-6666-6666-6666-666666666601");
    public static readonly Guid AlarmEventUid = Guid.Parse("66666666-6666-6666-6666-6666666666a1");
    public static readonly Guid FaultActive = Guid.Parse("66666666-6666-6666-6666-666666666602");
    public static readonly Guid FaultUid = Guid.Parse("66666666-6666-6666-6666-6666666666f1");
    public static readonly Guid LossReport = Guid.Parse("66666666-6666-6666-6666-666666666603");
    public static readonly Guid LossReportUid = Guid.Parse("66666666-6666-6666-6666-6666666666e1");

    public static readonly Guid CommandExecuted = Guid.Parse("77777777-7777-7777-7777-777777777701");
    public static readonly Guid CommandPending = Guid.Parse("77777777-7777-7777-7777-777777777702");
    public static readonly Guid IdempotencyKey = Guid.Parse("77777777-7777-7777-7777-7777777777aa");

    public static readonly Guid PushTokenOwnerA = Guid.Parse("88888888-8888-8888-8888-888888888801");
    public static readonly Guid AdminAlert = Guid.Parse("88888888-8888-8888-8888-888888888802");
    public static readonly Guid NotificationAlarm = Guid.Parse("88888888-8888-8888-8888-888888888803");
    public static readonly Guid NotificationFault = Guid.Parse("88888888-8888-8888-8888-888888888804");
    public static readonly Guid NotificationAdmin = Guid.Parse("88888888-8888-8888-8888-888888888805");

    public static readonly Guid OutboxCommand = Guid.Parse("99999999-9999-9999-9999-999999999901");
}
