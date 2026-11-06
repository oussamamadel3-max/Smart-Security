using System.Text.Json;

namespace Application.Dtos.AdminDtos;

/// <summary>
/// Admin request to register a device with independent MQTT credentials and approved settings (FR-003).
/// </summary>
public sealed class RegisterDeviceRequestDto
{
    public string HardwareUid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public RegisterDeviceCredentialDto Credential { get; set; } = new();
    public RegisterDeviceSettingsDto Settings { get; set; } = new();
}

public sealed class RegisterDeviceCredentialDto
{
    /// <summary>
    /// PASSWORD_HASH, CERTIFICATE, or SECRET_REF.
    /// </summary>
    public string AuthType { get; set; } = string.Empty;

    /// <summary>
    /// Plain MQTT password; hashed before persistence. Required when AuthType=PASSWORD_HASH. Never audited.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Certificate fingerprint. Required when AuthType=CERTIFICATE.
    /// </summary>
    public string? CertificateFingerprint { get; set; }

    /// <summary>
    /// External secret manager reference. Required when AuthType=SECRET_REF.
    /// </summary>
    public string? SecretRef { get; set; }
}

public sealed class RegisterDeviceSettingsDto
{
    /// <summary>
    /// Approved alarm/calibration JSON object for this device. Must be a JSON object (not an array/scalar).
    /// </summary>
    public JsonElement AlarmRule { get; set; }

    /// <summary>
    /// Approved positive event buffer capacity for this device (D-03 production default still open).
    /// </summary>
    public int EventBufferCapacity { get; set; }

    /// <summary>
    /// Approved positive command TTL seconds for this device (D-03 production default still open).
    /// </summary>
    public int CommandTtlSeconds { get; set; }
}

public sealed class RegisteredDeviceResponseDto
{
    public Guid Id { get; set; }
    public string HardwareUid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty;
    public long CurrentOwnershipVersion { get; set; }
    public long SettingsVersion { get; set; }
    public string CredentialAuthType { get; set; } = string.Empty;
    public bool HasActiveCredential { get; set; }
    public bool HasApprovedSettings { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
