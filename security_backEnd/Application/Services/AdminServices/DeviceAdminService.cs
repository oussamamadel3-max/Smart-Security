using System.Text.Json;
using Application.Dtos.AdminDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AdminServices;

public sealed class DeviceAdminService : IDeviceAdminService
{
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogService _audit;
    private readonly IDeviceCredentialHasher _credentialHasher;

    public DeviceAdminService(
        IDeviceRepository devices,
        IUnitOfWork uow,
        IAuditLogService audit,
        IDeviceCredentialHasher credentialHasher)
    {
        _devices = devices;
        _uow = uow;
        _audit = audit;
        _credentialHasher = credentialHasher;
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
