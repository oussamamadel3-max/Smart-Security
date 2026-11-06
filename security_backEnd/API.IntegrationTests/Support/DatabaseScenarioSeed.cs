using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Commands;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Support;

/// <summary>
/// بذرة مترابطة قابلة لإعادة الإنشاء تغطي جداول نظام الأمان الذكي (لا فروع/خدمات — خارج نطاق المخطط).
/// </summary>
public static class DatabaseScenarioSeed
{
    public sealed record Counts(
        int Users,
        int Roles,
        int UserRoles,
        int Devices,
        int Ownerships,
        int Credentials,
        int Sensors,
        int Settings,
        int States,
        int Commands,
        int CommandHistory,
        int Alarms,
        int Faults,
        int LossReports,
        int PushTokens,
        int AdminAlerts,
        int Notifications,
        int Deliveries,
        int AuditLogs,
        int Outbox);

    public static async Task<Counts> ApplyAsync(AppDbContext db, DateTimeOffset? clock = null)
    {
        var now = clock ?? new DateTimeOffset(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
        var earlier = now.AddDays(-7);

        db.Roles.AddRange(
            NewRole(SeedIds.RoleAdmin, DbStatusCodes.RoleCodes.Admin, "مسؤول النظام"),
            NewRole(SeedIds.RoleOwner, DbStatusCodes.RoleCodes.Owner, "مالك جهاز"));

        db.Users.AddRange(
            NewUser(SeedIds.AdminUser, "seed_admin", "Seed Admin", "admin@example.test", now, active: true),
            NewUser(SeedIds.OwnerA, "seed_owner_a", "Owner Alpha", "owner.a@example.test", now, active: true),
            NewUser(SeedIds.OwnerB, "seed_owner_b", "Owner Beta", "owner.b@example.test", now, active: true),
            NewUser(SeedIds.ArchivedUser, "seed_archived", "Archived User", null, earlier, active: false, deletedAt: earlier));

        db.UserRoles.AddRange(
            new UserRole { UserId = SeedIds.AdminUser, RoleId = SeedIds.RoleAdmin, AssignedAt = now, AssignedByUserId = null },
            new UserRole { UserId = SeedIds.OwnerA, RoleId = SeedIds.RoleOwner, AssignedAt = now, AssignedByUserId = SeedIds.AdminUser },
            new UserRole { UserId = SeedIds.OwnerB, RoleId = SeedIds.RoleOwner, AssignedAt = now, AssignedByUserId = SeedIds.AdminUser });

        await db.SaveChangesAsync();

        // جهاز ACTIVE بملكية منقولة: ملكية سابقة منتهية + ملكية حالية لـ OwnerA
        db.Devices.AddRange(
            new Device
            {
                Id = SeedIds.DeviceActive,
                HardwareUid = "hw-seed-active-001",
                DisplayName = "باب المستودع",
                LifecycleStatus = DbStatusCodes.DeviceLifecycle.Active,
                CurrentOwnershipVersion = 2,
                CreatedByUserId = SeedIds.AdminUser,
                CreatedAt = earlier,
                UpdatedAt = now
            },
            new Device
            {
                Id = SeedIds.DeviceRegistered,
                HardwareUid = "hw-seed-reg-002",
                DisplayName = "نافذة المكتب",
                LifecycleStatus = DbStatusCodes.DeviceLifecycle.Registered,
                CurrentOwnershipVersion = 0,
                CreatedByUserId = SeedIds.AdminUser,
                CreatedAt = now,
                UpdatedAt = now
            });

        db.DeviceStates.AddRange(
            NewState(SeedIds.DeviceActive, arm: DbStatusCodes.Arm.Armed, ownershipVersion: 2, settingsVersion: 1),
            NewState(SeedIds.DeviceRegistered, arm: DbStatusCodes.Arm.Unknown, ownershipVersion: 0, settingsVersion: 1));

        db.DeviceSettings.AddRange(
            NewSettings(SeedIds.DeviceActive, now),
            NewSettings(SeedIds.DeviceRegistered, now));

        db.DeviceOwnerships.AddRange(
            new DeviceOwnership
            {
                Id = SeedIds.OwnershipEnded,
                DeviceId = SeedIds.DeviceActive,
                OwnerUserId = SeedIds.OwnerB,
                Version = 1,
                Status = DbStatusCodes.Ownership.Ended,
                StartedAt = earlier,
                ActivatedAt = earlier,
                EndedAt = now.AddHours(-1),
                CreatedByUserId = SeedIds.AdminUser,
                EndedByUserId = SeedIds.AdminUser
            },
            new DeviceOwnership
            {
                Id = SeedIds.OwnershipActive,
                DeviceId = SeedIds.DeviceActive,
                OwnerUserId = SeedIds.OwnerA,
                Version = 2,
                Status = DbStatusCodes.Ownership.Active,
                StartedAt = now.AddHours(-1),
                ActivatedAt = now.AddHours(-1),
                CreatedByUserId = SeedIds.AdminUser
            });

        db.DeviceSensors.Add(new DeviceSensor
        {
            Id = SeedIds.SensorVib,
            DeviceId = SeedIds.DeviceActive,
            SensorKey = "vib-1",
            SensorType = DbStatusCodes.SensorType.Vibration,
            DisplayName = "اهتزاز الباب",
            IsEnabled = true,
            DiagnosticStatus = DbStatusCodes.Diagnostic.Healthy,
            LastSignalAt = now.AddMinutes(-5),
            Configuration = """{"schemaVersion":1,"note":"seed-only"}"""
        });

        db.DeviceCredentials.Add(new DeviceCredential
        {
            Id = SeedIds.Credential,
            DeviceId = SeedIds.DeviceActive,
            AuthType = DbStatusCodes.AuthType.PasswordHash,
            Verifier = "seed-verifier-not-a-real-secret",
            CreatedAt = earlier
        });

        await db.SaveChangesAsync();

        db.AlarmEvents.Add(new AlarmEvent
        {
            Id = SeedIds.AlarmActive,
            DeviceId = SeedIds.DeviceActive,
            EventUid = SeedIds.AlarmEventUid,
            OwnershipId = SeedIds.OwnershipActive,
            OwnershipVersion = 2,
            SensorId = SeedIds.SensorVib,
            SensorKeySnapshot = "vib-1",
            Status = DbStatusCodes.AlarmEvent.Active,
            ReceivedAt = now.AddMinutes(-3),
            ClockTrusted = true,
            OccurredDeviceAt = now.AddMinutes(-3),
            WasBuffered = false,
            SignalSummary = """{"peak":42}"""
        });

        db.DeviceFaults.Add(new DeviceFault
        {
            Id = SeedIds.FaultActive,
            DeviceId = SeedIds.DeviceActive,
            FaultUid = SeedIds.FaultUid,
            OwnershipId = SeedIds.OwnershipActive,
            OwnershipVersion = 2,
            SensorId = SeedIds.SensorVib,
            FaultCode = "SENSOR_READ_ERROR",
            Status = DbStatusCodes.Fault.Active,
            ReceivedAt = now.AddMinutes(-2),
            DeviceOccurredAt = now.AddMinutes(-2),
            Details = """{"code":"SENSOR_READ_ERROR"}"""
        });

        db.EventLossReports.Add(new EventLossReport
        {
            Id = SeedIds.LossReport,
            DeviceId = SeedIds.DeviceActive,
            ReportUid = SeedIds.LossReportUid,
            OwnershipId = SeedIds.OwnershipActive,
            OwnershipVersion = 2,
            Reason = DbStatusCodes.EventLossReason.BufferFull,
            LostCount = 3,
            ReceivedAt = now.AddMinutes(-10),
            DeviceOccurredAt = now.AddMinutes(-10)
        });

        await db.SaveChangesAsync();

        // أمر منفَّذ + أمر معلّق (أمر واحد PENDING لكل جهاز)
        db.DeviceCommands.AddRange(
            new DeviceCommand
            {
                Id = SeedIds.CommandExecuted,
                DeviceId = SeedIds.DeviceActive,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 2,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 1,
                CommandType = DbStatusCodes.CommandType.Arm,
                Status = DbStatusCodes.CommandStatus.Executed,
                CreatedAt = now.AddMinutes(-30),
                ExpiresAt = now.AddMinutes(-25),
                SentAt = now.AddMinutes(-29),
                AcknowledgedAt = now.AddMinutes(-28),
                IdempotencyKey = SeedIds.IdempotencyKey,
                ResultStateSequence = 5,
                ObservedStateSequence = 4
            },
            new DeviceCommand
            {
                Id = SeedIds.CommandPending,
                DeviceId = SeedIds.DeviceActive,
                OwnershipId = SeedIds.OwnershipActive,
                OwnershipVersion = 2,
                RequestedByUserId = SeedIds.OwnerA,
                CommandSequence = 2,
                CommandType = DbStatusCodes.CommandType.MuteAlarm,
                Status = DbStatusCodes.CommandStatus.Pending,
                CreatedAt = now.AddMinutes(-1),
                ExpiresAt = now.AddMinutes(4),
                AlarmEventId = SeedIds.AlarmActive,
                TargetAlarmEventUid = SeedIds.AlarmEventUid,
                ObservedStateSequence = 5
            });

        db.CommandStatusHistory.AddRange(
            new CommandStatusHistory
            {
                CommandId = SeedIds.CommandExecuted,
                FromStatus = null,
                ToStatus = DbStatusCodes.CommandStatus.Pending,
                ChangedAt = now.AddMinutes(-30),
                IsLateAck = false
            },
            new CommandStatusHistory
            {
                CommandId = SeedIds.CommandExecuted,
                FromStatus = DbStatusCodes.CommandStatus.Pending,
                ToStatus = DbStatusCodes.CommandStatus.Executed,
                ChangedAt = now.AddMinutes(-28),
                IsLateAck = false
            },
            new CommandStatusHistory
            {
                CommandId = SeedIds.CommandPending,
                FromStatus = null,
                ToStatus = DbStatusCodes.CommandStatus.Pending,
                ChangedAt = now.AddMinutes(-1),
                IsLateAck = false
            });

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = SeedIds.OutboxCommand,
            AggregateType = "DEVICE_COMMAND",
            AggregateId = SeedIds.CommandPending,
            MessageType = "PUBLISH_DEVICE_COMMAND",
            Payload = """{"commandId":"77777777-7777-7777-7777-777777777702","deviceId":"33333333-3333-3333-3333-333333333301"}""",
            CreatedAt = now.AddMinutes(-1),
            AvailableAt = now.AddMinutes(-1),
            AttemptCount = 0
        });

        await db.SaveChangesAsync();

        db.PushTokens.Add(new PushToken
        {
            Id = SeedIds.PushTokenOwnerA,
            UserId = SeedIds.OwnerA,
            TokenCiphertext = Convert.FromHexString("0102030405060708"),
            TokenFingerprint = Convert.FromHexString("AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899"),
            Platform = DbStatusCodes.PushPlatform.Android,
            RegisteredAt = earlier,
            LastUsedAt = now.AddMinutes(-3)
        });

        db.AdminAlerts.Add(new AdminAlert
        {
            Id = SeedIds.AdminAlert,
            SenderUserId = SeedIds.AdminUser,
            RecipientUserId = SeedIds.OwnerA,
            Title = "صيانة مجدولة",
            Body = "سيُعاد تشغيل الوسيط الليلة.",
            CreatedAt = now
        });

        await db.SaveChangesAsync();

        db.Notifications.AddRange(
            new Notification
            {
                Id = SeedIds.NotificationAlarm,
                RecipientUserId = SeedIds.OwnerA,
                NotificationType = DbStatusCodes.NotificationType.Alarm,
                AlarmEventId = SeedIds.AlarmActive,
                Status = DbStatusCodes.NotificationStatus.Accepted,
                CreatedAt = now.AddMinutes(-3),
                LastAttemptAt = now.AddMinutes(-3)
            },
            new Notification
            {
                Id = SeedIds.NotificationFault,
                RecipientUserId = SeedIds.OwnerA,
                NotificationType = DbStatusCodes.NotificationType.Fault,
                DeviceFaultId = SeedIds.FaultActive,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = now.AddMinutes(-2),
                NextAttemptAt = now.AddMinutes(1)
            },
            new Notification
            {
                Id = SeedIds.NotificationAdmin,
                RecipientUserId = SeedIds.OwnerA,
                NotificationType = DbStatusCodes.NotificationType.AdminAlert,
                AdminAlertId = SeedIds.AdminAlert,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = now
            });

        await db.SaveChangesAsync();

        db.NotificationDeliveries.Add(new NotificationDelivery
        {
            NotificationId = SeedIds.NotificationAlarm,
            PushTokenId = SeedIds.PushTokenOwnerA,
            AttemptNo = 1,
            AttemptedAt = now.AddMinutes(-3),
            ProviderResult = DbStatusCodes.ProviderResult.Accepted,
            ProviderMessageId = "fcm-seed-msg-1"
        });

        db.AuditLogs.AddRange(
            new AuditLog
            {
                ActorUserId = SeedIds.AdminUser,
                Action = "DEVICE_REGISTERED",
                TargetType = "DEVICE",
                TargetId = SeedIds.DeviceActive,
                DeviceId = SeedIds.DeviceActive,
                Result = DbStatusCodes.AuditResult.Success,
                OccurredAt = earlier,
                Details = """{"hardwareUid":"hw-seed-active-001"}"""
            },
            new AuditLog
            {
                ActorUserId = SeedIds.AdminUser,
                Action = "DEVICE_TRANSFERRED",
                TargetType = "DEVICE_OWNERSHIP",
                TargetId = SeedIds.OwnershipActive,
                DeviceId = SeedIds.DeviceActive,
                Result = DbStatusCodes.AuditResult.Success,
                OccurredAt = now.AddHours(-1),
                Details = """{"fromOwner":"owner_b","toOwner":"owner_a"}"""
            },
            new AuditLog
            {
                ActorUserId = SeedIds.OwnerA,
                Action = "COMMAND_REQUESTED",
                TargetType = "DEVICE_COMMAND",
                TargetId = SeedIds.CommandPending,
                DeviceId = SeedIds.DeviceActive,
                Result = DbStatusCodes.AuditResult.Success,
                OccurredAt = now.AddMinutes(-1)
            });

        await db.SaveChangesAsync();

        return new Counts(
            Users: await db.Users.CountAsync(),
            Roles: await db.Roles.CountAsync(),
            UserRoles: await db.UserRoles.CountAsync(),
            Devices: await db.Devices.CountAsync(),
            Ownerships: await db.DeviceOwnerships.CountAsync(),
            Credentials: await db.DeviceCredentials.CountAsync(),
            Sensors: await db.DeviceSensors.CountAsync(),
            Settings: await db.DeviceSettings.CountAsync(),
            States: await db.DeviceStates.CountAsync(),
            Commands: await db.DeviceCommands.CountAsync(),
            CommandHistory: await db.CommandStatusHistory.CountAsync(),
            Alarms: await db.AlarmEvents.CountAsync(),
            Faults: await db.DeviceFaults.CountAsync(),
            LossReports: await db.EventLossReports.CountAsync(),
            PushTokens: await db.PushTokens.CountAsync(),
            AdminAlerts: await db.AdminAlerts.CountAsync(),
            Notifications: await db.Notifications.CountAsync(),
            Deliveries: await db.NotificationDeliveries.CountAsync(),
            AuditLogs: await db.AuditLogs.CountAsync(),
            Outbox: await db.OutboxMessages.CountAsync());
    }

    private static Role NewRole(Guid id, string code, string display) => new()
    {
        Id = id,
        Name = code,
        NormalizedName = code.ToUpperInvariant(),
        DisplayName = display,
        ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };

    private static User NewUser(
        Guid id,
        string userName,
        string fullName,
        string? email,
        DateTimeOffset now,
        bool active,
        DateTimeOffset? deletedAt = null) => new()
    {
        Id = id,
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email?.ToUpperInvariant(),
        FullName = fullName,
        IsActive = active,
        CreatedAt = now,
        UpdatedAt = now,
        DeletedAt = deletedAt,
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        EmailConfirmed = email is not null,
        PhoneNumberConfirmed = false,
        TwoFactorEnabled = false,
        LockoutEnabled = true,
        AccessFailedCount = 0
    };

    private static DeviceState NewState(Guid deviceId, string arm, long ownershipVersion, long settingsVersion) => new()
    {
        DeviceId = deviceId,
        StateSequence = ownershipVersion == 0 ? 0 : 5,
        ArmState = arm,
        SirenState = DbStatusCodes.Siren.Off,
        HealthState = DbStatusCodes.Diagnostic.Healthy,
        BatteryPercent = 88,
        PowerSource = DbStatusCodes.PowerSource.Mains,
        ConfirmedOwnershipVersion = ownershipVersion,
        AppliedSettingsVersion = settingsVersion,
        ClockTrusted = ownershipVersion > 0,
        LastSeenAt = ownershipVersion > 0 ? DateTimeOffset.UtcNow : null,
        LastStateAt = ownershipVersion > 0 ? DateTimeOffset.UtcNow : null
    };

    private static DeviceSettings NewSettings(Guid deviceId, DateTimeOffset now) => new()
    {
        DeviceId = deviceId,
        Version = 1,
        AlarmRule = """{"schemaVersion":1,"note":"seed-only-not-production"}""",
        TestDurationMs = 30000,
        HeartbeatTimeoutSeconds = 3600,
        CommandTimeoutSeconds = 10,
        EventBufferCapacity = 32,
        CommandTtlSeconds = 60,
        UpdatedAt = now,
        UpdatedByUserId = SeedIds.AdminUser
    };
}
