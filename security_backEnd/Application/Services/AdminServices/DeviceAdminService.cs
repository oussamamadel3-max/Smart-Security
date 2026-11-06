using System.Text.Json;
using Application.Dtos.AdminDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Entities.Operations;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdminServices;

public sealed class DeviceAdminService : IDeviceAdminService
{
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly IDeviceCredentialHasher _credentialHasher;
    private readonly IDeviceBrokerAccessService _brokerAccess;

    public DeviceAdminService(
        IDeviceRepository devices,
        IUnitOfWork uow,
        IAuditLogService audit,
        IDeviceCredentialHasher credentialHasher,
        IDeviceBrokerAccessService brokerAccess)
    {
        _devices = devices;
        _uow = uow;
        _audit = audit;
        _credentialHasher = credentialHasher;
        _brokerAccess = brokerAccess;
    }

    public async Task<RegisteredDeviceResponseDto> RegisterAsync(
        RegisterDeviceRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var hardwareUid = NormalizeRequired(request.HardwareUid, 128, "معرف الجهاز غير صالح.", "INVALID_HARDWARE_UID");
        var displayName = NormalizeRequired(request.DisplayName, 150, "اسم الجهاز غير صالح.", "INVALID_DISPLAY_NAME");
        var alarmRuleJson = NormalizeAlarmRule(request.Settings.AlarmRule);
        ValidateApprovedCapacity(request.Settings);
        var credentialParts = BuildCredentialParts(request.Credential);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            if (await _devices.ExistsByHardwareUidAsync(hardwareUid, ct))
                throw new ConflictException("معرف الجهاز مستخدم مسبقاً.", "DUPLICATE_HARDWARE_UID");

            var now = DateTimeOffset.UtcNow;
            var deviceId = Guid.NewGuid();

            var device = new Device
            {
                Id = deviceId,
                HardwareUid = hardwareUid,
                DisplayName = displayName,
                LifecycleStatus = DbStatusCodes.DeviceLifecycle.Registered,
                CurrentOwnershipVersion = 0,
                CreatedByUserId = actorUserId,
                CreatedAt = now,
                UpdatedAt = now,
                RetiredAt = null
            };

            var state = new DeviceState
            {
                DeviceId = deviceId,
                StateSequence = 0,
                ArmState = DbStatusCodes.Arm.Unknown,
                SirenState = DbStatusCodes.Siren.Unknown,
                HealthState = DbStatusCodes.Diagnostic.Unknown,
                ConfirmedOwnershipVersion = 0,
                AppliedSettingsVersion = 0,
                ClockTrusted = false
            };

            var settings = new DeviceSettings
            {
                DeviceId = deviceId,
                Version = ApprovedDeviceSettings.InitialSettingsVersion,
                AlarmRule = alarmRuleJson,
                TestDurationMs = ApprovedDeviceSettings.TestDurationMs,
                HeartbeatTimeoutSeconds = ApprovedDeviceSettings.HeartbeatTimeoutSeconds,
                CommandTimeoutSeconds = ApprovedDeviceSettings.CommandTimeoutSeconds,
                EventBufferCapacity = request.Settings.EventBufferCapacity,
                CommandTtlSeconds = request.Settings.CommandTtlSeconds,
                UpdatedAt = now,
                UpdatedByUserId = actorUserId
            };

            var credential = new DeviceCredential
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                AuthType = credentialParts.AuthType,
                Verifier = credentialParts.Verifier,
                CertificateFingerprint = credentialParts.CertificateFingerprint,
                SecretRef = credentialParts.SecretRef,
                CreatedAt = now,
                RevokedAt = null
            };

            _devices.AddRegistrationGraph(device, state, settings, credential);

            await _audit.WriteAsync(
                actorUserId,
                AuditActions.DeviceRegistered,
                AuditActions.TargetTypes.Device,
                deviceId,
                DbStatusCodes.AuditResult.Success,
                new
                {
                    hardwareUid,
                    displayName,
                    lifecycleStatus = device.LifecycleStatus,
                    settingsVersion = settings.Version,
                    credentialAuthType = credential.AuthType,
                    testDurationMs = settings.TestDurationMs,
                    heartbeatTimeoutSeconds = settings.HeartbeatTimeoutSeconds,
                    commandTimeoutSeconds = settings.CommandTimeoutSeconds,
                    eventBufferCapacity = settings.EventBufferCapacity,
                    commandTtlSeconds = settings.CommandTtlSeconds
                },
                deviceId,
                ct);

            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueHardwareUidViolation(ex))
            {
                throw new ConflictException("معرف الجهاز مستخدم مسبقاً.", "DUPLICATE_HARDWARE_UID");
            }

            await tx.CommitAsync(ct);

            return new RegisteredDeviceResponseDto
            {
                Id = device.Id,
                HardwareUid = device.HardwareUid,
                DisplayName = device.DisplayName,
                LifecycleStatus = device.LifecycleStatus,
                CurrentOwnershipVersion = device.CurrentOwnershipVersion,
                SettingsVersion = settings.Version,
                CredentialAuthType = credential.AuthType,
                HasActiveCredential = true,
                HasApprovedSettings = true,
                CreatedAt = device.CreatedAt
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<ArchivedDeviceResponseDto> ArchiveAsync(
        Guid deviceId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var device = await _devices.GetByIdAsync(deviceId, ct)
                ?? throw new NotFoundException("الجهاز غير موجود.", "DEVICE_NOT_FOUND");

            if (device.LifecycleStatus == DbStatusCodes.DeviceLifecycle.Retired)
                throw new ConflictException("الجهاز مؤرشف مسبقاً.", "DEVICE_ALREADY_RETIRED");

            var now = DateTimeOffset.UtcNow;
            var openOwnership = await _devices.GetOpenOwnershipAsync(deviceId, ct);
            var ownershipEnded = false;
            string? closedOwnershipStatus = null;
            Guid? closedOwnershipId = null;
            var ownershipSyncQueued = false;

            if (openOwnership is not null)
            {
                var closedStatus = openOwnership.Status == DbStatusCodes.Ownership.Pending
                    ? DbStatusCodes.Ownership.Cancelled
                    : DbStatusCodes.Ownership.Ended;

                openOwnership.Status = closedStatus;
                openOwnership.EndedAt = now;
                openOwnership.EndedByUserId = actorUserId;

                device.CurrentOwnershipVersion += 1;
                ownershipEnded = true;
                closedOwnershipStatus = closedStatus;
                closedOwnershipId = openOwnership.Id;

                // New aggregate id: ux_outbox_messages_type_aggregate already holds the link-time row for this ownership.
                var unlinkAggregateId = Guid.NewGuid();
                var messageId = Guid.NewGuid();
                var payload = BuildOwnershipUnlinkPayload(
                    deviceId,
                    messageId,
                    device.CurrentOwnershipVersion);

                _devices.AddOutboxMessage(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateType = OwnershipSyncDraft.OutboxAggregateType,
                    AggregateId = unlinkAggregateId,
                    MessageType = OwnershipSyncDraft.OutboxPublishMessageType,
                    Payload = payload,
                    CreatedAt = now,
                    AvailableAt = now,
                    ProcessedAt = null,
                    AttemptCount = 0,
                    LastErrorCode = null
                });
                ownershipSyncQueued = true;
            }

            var credentials = await _devices.ListCredentialsAsync(deviceId, ct);
            var credentialsRevoked = 0;
            foreach (var credential in credentials)
            {
                if (credential.RevokedAt is not null)
                    continue;
                credential.RevokedAt = now;
                credentialsRevoked++;
            }

            device.LifecycleStatus = DbStatusCodes.DeviceLifecycle.Retired;
            device.RetiredAt = now;
            device.UpdatedAt = now;

            await _audit.WriteAsync(
                actorUserId,
                AuditActions.DeviceArchived,
                AuditActions.TargetTypes.Device,
                deviceId,
                DbStatusCodes.AuditResult.Success,
                new
                {
                    hardwareUid = device.HardwareUid,
                    displayName = device.DisplayName,
                    lifecycleStatus = device.LifecycleStatus,
                    retiredAt = now,
                    ownershipEnded,
                    closedOwnershipStatus,
                    closedOwnershipId,
                    ownershipVersion = device.CurrentOwnershipVersion,
                    ownershipSyncQueued,
                    credentialsRevoked,
                    brokerAccessRevokeRequired = true
                },
                deviceId,
                ct);

            await _uow.SaveChangesAsync(ct);

            // FR-007: broker ACL/session revoke must succeed before archive is declared complete.
            try
            {
                await _brokerAccess.RevokeAccessAsync(device.Id, device.HardwareUid, ct);
            }
            catch (ServiceUnavailableException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ServiceUnavailableException(
                    "تعذر إبطال وصول الجهاز لدى وسيط MQTT.",
                    "DEVICE_BROKER_REVOKE_FAILED");
            }

            await tx.CommitAsync(ct);

            return new ArchivedDeviceResponseDto
            {
                Id = device.Id,
                HardwareUid = device.HardwareUid,
                DisplayName = device.DisplayName,
                LifecycleStatus = device.LifecycleStatus,
                RetiredAt = now,
                CurrentOwnershipVersion = device.CurrentOwnershipVersion,
                OwnershipEnded = ownershipEnded,
                ClosedOwnershipStatus = closedOwnershipStatus,
                ClosedOwnershipId = closedOwnershipId,
                OwnershipSyncQueued = ownershipSyncQueued,
                CredentialsRevoked = credentialsRevoked,
                HasActiveCredential = false,
                BrokerAccessRevoked = true
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static string BuildOwnershipUnlinkPayload(
        Guid deviceId,
        Guid messageId,
        long ownershipVersion)
    {
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = OwnershipSyncDraft.SchemaVersion,
            ["deviceId"] = deviceId,
            ["messageId"] = messageId,
            ["messageType"] = OwnershipSyncDraft.MqttMessageType,
            ["ownershipVersion"] = ownershipVersion,
            ["isLinked"] = false
        };

        return JsonSerializer.Serialize(payload);
    }

    private CredentialParts BuildCredentialParts(RegisterDeviceCredentialDto credential)
    {
        if (credential is null)
            throw new BadRequestException("بيانات اعتماد الجهاز مطلوبة.", "CREDENTIAL_REQUIRED");

        var authType = (credential.AuthType ?? string.Empty).Trim().ToUpperInvariant();
        return authType switch
        {
            DbStatusCodes.AuthType.PasswordHash => BuildPasswordHash(credential.Password),
            DbStatusCodes.AuthType.Certificate => BuildCertificate(credential.CertificateFingerprint),
            DbStatusCodes.AuthType.SecretRef => BuildSecretRef(credential.SecretRef),
            _ => throw new BadRequestException("نوع اعتماد الجهاز غير مدعوم.", "INVALID_AUTH_TYPE")
        };
    }

    private CredentialParts BuildPasswordHash(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 256)
            throw new BadRequestException("كلمة مرور اعتماد الجهاز غير مقبولة.", "INVALID_DEVICE_PASSWORD");

        return new CredentialParts(
            DbStatusCodes.AuthType.PasswordHash,
            _credentialHasher.HashPassword(password),
            null,
            null);
    }

    private static CredentialParts BuildCertificate(string? fingerprint)
    {
        var value = fingerprint?.Trim() ?? string.Empty;
        if (value.Length is < 8 or > 128)
            throw new BadRequestException("بصمة الشهادة غير صالحة.", "INVALID_CERTIFICATE_FINGERPRINT");

        return new CredentialParts(DbStatusCodes.AuthType.Certificate, null, value, null);
    }

    private static CredentialParts BuildSecretRef(string? secretRef)
    {
        var value = secretRef?.Trim() ?? string.Empty;
        if (value.Length is < 1 or > 512)
            throw new BadRequestException("مرجع السر غير صالح.", "INVALID_SECRET_REF");

        return new CredentialParts(DbStatusCodes.AuthType.SecretRef, null, null, value);
    }

    private static void ValidateApprovedCapacity(RegisterDeviceSettingsDto settings)
    {
        if (settings is null)
            throw new BadRequestException("إعدادات الجهاز المعتمدة مطلوبة.", "SETTINGS_REQUIRED");

        if (settings.EventBufferCapacity <= 0)
            throw new BadRequestException(
                "سعة مخزن الأحداث يجب أن تكون موجبة ومعتمدة لهذا الجهاز.",
                "INVALID_EVENT_BUFFER_CAPACITY");

        if (settings.CommandTtlSeconds <= 0)
            throw new BadRequestException(
                "مدة صلاحية الأمر يجب أن تكون موجبة ومعتمدة لهذا الجهاز.",
                "INVALID_COMMAND_TTL");
    }

    private static string NormalizeAlarmRule(JsonElement alarmRule)
    {
        if (alarmRule.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new BadRequestException("قاعدة الإنذار المعتمدة مطلوبة ككائن JSON.", "ALARM_RULE_REQUIRED");

        if (alarmRule.ValueKind != JsonValueKind.Object)
            throw new BadRequestException("قاعدة الإنذار يجب أن تكون كائن JSON.", "INVALID_ALARM_RULE");

        return alarmRule.GetRawText();
    }

    private static string NormalizeRequired(string? value, int maxLength, string message, string code)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 || trimmed.Length > maxLength)
            throw new BadRequestException(message, code);
        return trimmed;
    }

    private static bool IsUniqueHardwareUidViolation(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("ux_devices_hardware_uid", StringComparison.OrdinalIgnoreCase)
               || text.Contains("hardware_uid", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CredentialParts(
        string AuthType,
        string? Verifier,
        string? CertificateFingerprint,
        string? SecretRef);
}
