using API.IntegrationTests.Support;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Commands;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests;

/// <summary>
/// المرحلتان 3 و4: اختبارات إيجابية/سلبية للقيود مع عزل عبر Transaction + Rollback.
/// </summary>
[Collection("PostgresDb")]
public sealed class DatabaseConstraintTests
{
    private readonly PostgresDbFixture _fx;

    public DatabaseConstraintTests(PostgresDbFixture fx) => _fx = fx;

    [Fact]
    public async Task Rejects_SecondOpenOwnership_ForSameDevice()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.DeviceOwnerships.Add(new DeviceOwnership
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                OwnerUserId = SeedIds.OwnerB,
                Version = 99,
                Status = DbStatusCodes.Ownership.Pending,
                StartedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = SeedIds.AdminUser
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_DuplicateAlarmEventUid_PerDevice()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.AlarmEvents.Add(new AlarmEvent
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                EventUid = SeedIds.AlarmEventUid,
                OwnershipVersion = 2,
                SensorKeySnapshot = "vib-1",
                Status = DbStatusCodes.AlarmEvent.Active,
                ReceivedAt = DateTimeOffset.UtcNow,
                ClockTrusted = false,
                WasBuffered = false
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_SecondPendingCommand_ForSameDevice()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.DeviceCommands.Add(new DeviceCommand
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 2,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 50,
                CommandType = DbStatusCodes.CommandType.Disarm,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                ObservedStateSequence = 5
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_CrossDevice_OwnershipForeignKey_OnCommand()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            // OwnershipActive ينتمي لـ DeviceActive — ربطه بجهاز آخر يجب أن يفشل
            _fx.Db.DeviceCommands.Add(new DeviceCommand
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceRegistered,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 0,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 1,
                CommandType = DbStatusCodes.CommandType.Arm,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                ObservedStateSequence = 0
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Blank_Username()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Users.Add(NewTransientUser("   ", "Blank User"));
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Blank_FullName()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Users.Add(NewTransientUser($"u_{Guid.NewGuid():N}"[..16], "   "));
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Active_Duplicate_Username_CaseInsensitive()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Users.Add(NewTransientUser("SEED_OWNER_A", "Dup"));
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Allows_Reuse_Username_After_SoftDelete()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            // seed_archived مؤرشف — إعادة استخدام الاسم يجب أن تنجح
            _fx.Db.Users.Add(NewTransientUser("seed_archived", "Reused Name"));
            await _fx.Db.SaveChangesAsync();
            Assert.True(await _fx.Db.Users.CountAsync(u => u.UserName!.ToLower() == "seed_archived") >= 2);
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_DeletedAt_With_IsActive_True()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            var u = NewTransientUser($"del_{Guid.NewGuid():N}"[..16], "Bad Archive");
            u.IsActive = true;
            u.DeletedAt = DateTimeOffset.UtcNow;
            _fx.Db.Users.Add(u);
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Invalid_Lifecycle_Status()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            var id = Guid.NewGuid();
            _fx.Db.Devices.Add(new Device
            {
                Id = id,
                HardwareUid = $"hw-bad-{id:N}"[..20],
                DisplayName = "Bad",
                LifecycleStatus = "ONLINE",
                CurrentOwnershipVersion = 0,
                CreatedByUserId = SeedIds.AdminUser,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Credential_Fields_Mismatch_AuthType()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.DeviceCredentials.Add(new DeviceCredential
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                AuthType = DbStatusCodes.AuthType.PasswordHash,
                Verifier = null,
                CertificateFingerprint = "should-not-be-set",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_MuteAlarm_Without_TargetUid()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            // أغلق الأمر المعلّق الحالي أولاً داخل المعاملة لإفساح فهرس PENDING
            var pending = await _fx.Db!.DeviceCommands.SingleAsync(c => c.Id == SeedIds.CommandPending);
            pending.Status = DbStatusCodes.CommandStatus.Cancelled;

            _fx.Db.DeviceCommands.Add(new DeviceCommand
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 2,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 90,
                CommandType = DbStatusCodes.CommandType.MuteAlarm,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                TargetAlarmEventUid = null,
                ObservedStateSequence = 5
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Notification_With_Multiple_Sources()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = SeedIds.OwnerA,
                NotificationType = DbStatusCodes.NotificationType.Alarm,
                AlarmEventId = SeedIds.AlarmActive,
                DeviceFaultId = SeedIds.FaultActive,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Delete_User_Referenced_By_Device()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            var admin = await _fx.Db!.Users.SingleAsync(u => u.Id == SeedIds.AdminUser);
            _fx.Db.Users.Remove(admin);
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Missing_ForeignKey_User()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Devices.Add(new Device
            {
                Id = Guid.NewGuid(),
                HardwareUid = $"hw-fk-{Guid.NewGuid():N}"[..20],
                DisplayName = "Orphan creator",
                LifecycleStatus = DbStatusCodes.DeviceLifecycle.Registered,
                CurrentOwnershipVersion = 0,
                CreatedByUserId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Username_Longer_Than_120()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Users.Add(NewTransientUser(new string('x', 121), "Long"));
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Battery_Out_Of_Range()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            var state = await _fx.Db!.DeviceStates.SingleAsync(s => s.DeviceId == SeedIds.DeviceActive);
            state.BatteryPercent = 150;
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_ExpiresAt_Before_CreatedAt()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            var pending = await _fx.Db!.DeviceCommands.SingleAsync(c => c.Id == SeedIds.CommandPending);
            pending.Status = DbStatusCodes.CommandStatus.Cancelled;
            var created = DateTimeOffset.UtcNow;
            _fx.Db.DeviceCommands.Add(new DeviceCommand
            {
                Id = Guid.NewGuid(),
                DeviceId = SeedIds.DeviceActive,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 2,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 91,
                CommandType = DbStatusCodes.CommandType.Arm,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = created,
                ExpiresAt = created.AddSeconds(-1),
                ObservedStateSequence = 5
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Rejects_Invalid_Audit_Result()
    {
        _fx.RequireDbOrFail();
        await using var tx = await _fx.Db!.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = SeedIds.AdminUser,
                Action = "X",
                TargetType = "DEVICE",
                Result = "OK",
                OccurredAt = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }
    }

    [Fact]
    public async Task Negative_Tests_Do_Not_Remove_Seed_Anchors()
    {
        _fx.RequireDbOrFail();
        // الاختبارات السلبية تُعزل بـ Rollback؛ نقاط البذرة الأساسية تبقى موجودة.
        Assert.True(await _fx.Db!.Users.AnyAsync(u => u.Id == SeedIds.AdminUser));
        Assert.True(await _fx.Db.Devices.AnyAsync(d => d.Id == SeedIds.DeviceActive));
        Assert.True(await _fx.Db.DeviceCommands.AnyAsync(c => c.Id == SeedIds.CommandPending));
        Assert.True(await _fx.Db.Notifications.AnyAsync(n => n.Id == SeedIds.NotificationAlarm));
        Assert.False(await _fx.Db.Users.AnyAsync(u => u.UserName == "   "));
    }

    private static User NewTransientUser(string userName, string fullName)
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.Trim().ToUpperInvariant(),
            FullName = fullName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0
        };
    }
}
