using API.IntegrationTests.Support;
using Domain.Constants;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests;

/// <summary>
/// المرحلة 4: المعاملات وRollback وسلامة التحديث المترابط.
/// </summary>
[Collection("PostgresDb")]
public sealed class DatabaseTransactionTests
{
    private readonly PostgresDbFixture _fx;

    public DatabaseTransactionTests(PostgresDbFixture fx) => _fx = fx;

    [Fact]
    public async Task Rollback_Discards_Partial_Device_Insert()
    {
        _fx.RequireDbOrFail();
        var before = await _fx.Db!.Devices.CountAsync();
        var newId = Guid.NewGuid();

        await using var tx = await _fx.Db.Database.BeginTransactionAsync();
        _fx.Db.Devices.Add(new Device
        {
            Id = newId,
            HardwareUid = $"hw-tx-{newId:N}"[..20],
            DisplayName = "Temp",
            LifecycleStatus = DbStatusCodes.DeviceLifecycle.Registered,
            CurrentOwnershipVersion = 0,
            CreatedByUserId = SeedIds.AdminUser,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await _fx.Db.SaveChangesAsync();
        Assert.Equal(before + 1, await _fx.Db.Devices.CountAsync());
        await tx.RollbackAsync();
        _fx.Db.ChangeTracker.Clear();

        Assert.Equal(before, await _fx.Db.Devices.CountAsync());
        Assert.False(await _fx.Db.Devices.AnyAsync(d => d.Id == newId));
    }

    [Fact]
    public async Task OwnershipTransfer_PreservesHistory_InSingleTransaction()
    {
        _fx.RequireDbOrFail();
        // إنشاء جهاز مستقل داخل معاملة ثم commit لنقل ملكية نظيف
        var deviceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var ownership1 = Guid.NewGuid();

        _fx.Db!.Devices.Add(new Device
        {
            Id = deviceId,
            HardwareUid = $"hw-xfer-{deviceId:N}"[..20],
            DisplayName = "Transfer Device",
            LifecycleStatus = DbStatusCodes.DeviceLifecycle.Active,
            CurrentOwnershipVersion = 1,
            CreatedByUserId = SeedIds.AdminUser,
            CreatedAt = now,
            UpdatedAt = now
        });
        _fx.Db.DeviceStates.Add(new DeviceState
        {
            DeviceId = deviceId,
            StateSequence = 0,
            ArmState = DbStatusCodes.Arm.Unknown,
            SirenState = DbStatusCodes.Siren.Unknown,
            HealthState = DbStatusCodes.Diagnostic.Unknown,
            ConfirmedOwnershipVersion = 0,
            AppliedSettingsVersion = 0,
            ClockTrusted = false
        });
        _fx.Db.DeviceSettings.Add(new DeviceSettings
        {
            DeviceId = deviceId,
            Version = 1,
            AlarmRule = """{"schemaVersion":1}""",
            TestDurationMs = 30000,
            HeartbeatTimeoutSeconds = 3600,
            CommandTimeoutSeconds = 10,
            EventBufferCapacity = 1,
            CommandTtlSeconds = 1,
            UpdatedAt = now,
            UpdatedByUserId = SeedIds.AdminUser
        });
        _fx.Db.DeviceOwnerships.Add(new DeviceOwnership
        {
            Id = ownership1,
            DeviceId = deviceId,
            OwnerUserId = SeedIds.OwnerA,
            Version = 1,
            Status = DbStatusCodes.Ownership.Active,
            StartedAt = now,
            ActivatedAt = now,
            CreatedByUserId = SeedIds.AdminUser
        });
        await _fx.Db.SaveChangesAsync();

        await using var tx = await _fx.Db.Database.BeginTransactionAsync();
        var old = await _fx.Db.DeviceOwnerships.SingleAsync(o => o.Id == ownership1);
        old.Status = DbStatusCodes.Ownership.Ended;
        old.EndedAt = now.AddMinutes(1);
        old.EndedByUserId = SeedIds.AdminUser;

        _fx.Db.DeviceOwnerships.Add(new DeviceOwnership
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            OwnerUserId = SeedIds.OwnerB,
            Version = 2,
            Status = DbStatusCodes.Ownership.Active,
            StartedAt = now.AddMinutes(1),
            ActivatedAt = now.AddMinutes(1),
            CreatedByUserId = SeedIds.AdminUser
        });

        var device = await _fx.Db.Devices.SingleAsync(d => d.Id == deviceId);
        device.CurrentOwnershipVersion = 2;
        device.UpdatedAt = now.AddMinutes(1);

        await _fx.Db.SaveChangesAsync();
        await tx.CommitAsync();

        var open = await _fx.Db.DeviceOwnerships.Where(o => o.DeviceId == deviceId && o.EndedAt == null).ToListAsync();
        Assert.Single(open);
        Assert.Equal(SeedIds.OwnerB, open[0].OwnerUserId);

        var history = await _fx.Db.DeviceOwnerships.Where(o => o.DeviceId == deviceId).OrderBy(o => o.Version).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(DbStatusCodes.Ownership.Ended, history[0].Status);
        Assert.Equal(DbStatusCodes.Ownership.Active, history[1].Status);
    }

    [Fact]
    public async Task Failed_Constraint_Inside_Transaction_Leaves_Seed_Intact_After_Rollback()
    {
        _fx.RequireDbOrFail();
        var usersBefore = await _fx.Db!.Users.CountAsync();

        await using var tx = await _fx.Db.Database.BeginTransactionAsync();
        try
        {
            _fx.Db.Users.Add(new Domain.Entities.Auth.User
            {
                Id = Guid.NewGuid(),
                UserName = "seed_owner_a",
                NormalizedUserName = "SEED_OWNER_A",
                FullName = "Clash",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => _fx.Db.SaveChangesAsync());
        }
        finally
        {
            await tx.RollbackAsync();
            _fx.Db.ChangeTracker.Clear();
        }

        Assert.Equal(usersBefore, await _fx.Db.Users.CountAsync());
    }
}
