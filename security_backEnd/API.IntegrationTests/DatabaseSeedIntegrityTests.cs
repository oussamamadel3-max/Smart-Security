using API.IntegrationTests.Support;
using Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests;

/// <summary>
/// المرحلتان 2 و5: سلامة البذرة المترابطة وسيناريوهات الأعمال على مستوى القاعدة.
/// </summary>
[Collection("PostgresDb")]
public sealed class DatabaseSeedIntegrityTests
{
    private readonly PostgresDbFixture _fx;

    public DatabaseSeedIntegrityTests(PostgresDbFixture fx) => _fx = fx;

    [Fact]
    public void Seed_Populates_All_Core_Tables()
    {
        _fx.RequireDbOrFail();
        var c = _fx.SeedCounts!;
        Assert.Equal(4, c.Users);
        Assert.Equal(2, c.Roles);
        Assert.Equal(3, c.UserRoles);
        Assert.Equal(2, c.Devices);
        Assert.Equal(2, c.Ownerships);
        Assert.Equal(1, c.Credentials);
        Assert.Equal(1, c.Sensors);
        Assert.Equal(2, c.Settings);
        Assert.Equal(2, c.States);
        Assert.Equal(2, c.Commands);
        Assert.Equal(3, c.CommandHistory);
        Assert.Equal(1, c.Alarms);
        Assert.Equal(1, c.Faults);
        Assert.Equal(1, c.LossReports);
        Assert.Equal(1, c.PushTokens);
        Assert.Equal(1, c.AdminAlerts);
        Assert.Equal(3, c.Notifications);
        Assert.Equal(1, c.Deliveries);
        Assert.Equal(3, c.AuditLogs);
        Assert.Equal(1, c.Outbox);
    }

    [Fact]
    public async Task Join_ActiveOwnership_Alarm_Notification_Is_Consistent()
    {
        _fx.RequireDbOrFail();
        var db = _fx.Db!;

        var row = await (
            from d in db.Devices
            join o in db.DeviceOwnerships on d.Id equals o.DeviceId
            join a in db.AlarmEvents on new { d.Id, OwnershipId = (Guid?)o.Id } equals new { Id = a.DeviceId, a.OwnershipId }
            join n in db.Notifications on a.Id equals n.AlarmEventId
            where d.Id == SeedIds.DeviceActive
                  && o.Status == DbStatusCodes.Ownership.Active
                  && n.NotificationType == DbStatusCodes.NotificationType.Alarm
            select new { d.HardwareUid, o.OwnerUserId, a.EventUid, n.RecipientUserId, n.Status }
        ).SingleAsync();

        Assert.Equal("hw-seed-active-001", row.HardwareUid);
        Assert.Equal(SeedIds.OwnerA, row.OwnerUserId);
        Assert.Equal(SeedIds.OwnerA, row.RecipientUserId);
        Assert.Equal(DbStatusCodes.NotificationStatus.Accepted, row.Status);
    }

    [Fact]
    public async Task Ownership_History_Keeps_Ended_And_Active()
    {
        _fx.RequireDbOrFail();
        var history = await _fx.Db!.DeviceOwnerships
            .Where(o => o.DeviceId == SeedIds.DeviceActive)
            .OrderBy(o => o.Version)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(DbStatusCodes.Ownership.Ended, history[0].Status);
        Assert.Equal(SeedIds.OwnerB, history[0].OwnerUserId);
        Assert.NotNull(history[0].EndedAt);
        Assert.Equal(DbStatusCodes.Ownership.Active, history[1].Status);
        Assert.Equal(SeedIds.OwnerA, history[1].OwnerUserId);
        Assert.Null(history[1].EndedAt);
    }

    [Fact]
    public async Task Command_History_Tracks_State_Transitions()
    {
        _fx.RequireDbOrFail();
        var steps = await _fx.Db!.CommandStatusHistory
            .Where(h => h.CommandId == SeedIds.CommandExecuted)
            .OrderBy(h => h.ChangedAt)
            .Select(h => h.ToStatus)
            .ToListAsync();

        Assert.Equal(
            new[] { DbStatusCodes.CommandStatus.Pending, DbStatusCodes.CommandStatus.Executed },
            steps);
    }

    [Fact]
    public async Task Roles_Assigned_To_Users()
    {
        _fx.RequireDbOrFail();
        var adminRoles = await _fx.Db!.UserRoles.Where(r => r.UserId == SeedIds.AdminUser).ToListAsync();
        var ownerRoles = await _fx.Db.UserRoles.Where(r => r.UserId == SeedIds.OwnerA).ToListAsync();
        Assert.Contains(adminRoles, r => r.RoleId == SeedIds.RoleAdmin);
        Assert.Contains(ownerRoles, r => r.RoleId == SeedIds.RoleOwner);
    }

    [Fact]
    public async Task Outbox_Tied_To_Pending_Command()
    {
        _fx.RequireDbOrFail();
        var msg = await _fx.Db!.OutboxMessages.SingleAsync(m => m.Id == SeedIds.OutboxCommand);
        Assert.Equal(SeedIds.CommandPending, msg.AggregateId);
        Assert.Null(msg.ProcessedAt);
        Assert.Equal(0, msg.AttemptCount);
    }

    [Fact]
    public async Task Archived_User_Is_Inactive_With_DeletedAt()
    {
        _fx.RequireDbOrFail();
        var u = await _fx.Db!.Users.SingleAsync(x => x.Id == SeedIds.ArchivedUser);
        Assert.False(u.IsActive);
        Assert.NotNull(u.DeletedAt);
    }

    [Fact]
    public async Task No_Orphan_Notifications_Without_Source()
    {
        _fx.RequireDbOrFail();
        var orphans = await _fx.Db!.Notifications
            .Where(n => n.AlarmEventId == null && n.DeviceFaultId == null && n.AdminAlertId == null)
            .CountAsync();
        Assert.Equal(0, orphans);
    }
}
