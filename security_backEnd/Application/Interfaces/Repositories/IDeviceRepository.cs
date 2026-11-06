using Domain.Entities.Devices;
using Domain.Entities.Operations;

namespace Application.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task<bool> ExistsByHardwareUidAsync(string hardwareUid, CancellationToken ct = default);

    void AddRegistrationGraph(
        Device device,
        DeviceState state,
        DeviceSettings settings,
        DeviceCredential credential);

    Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Loads <c>devices</c> with PostgreSQL <c>FOR UPDATE</c> inside the ambient transaction (transfer/unlink).
    /// </summary>
    Task<Device?> GetByIdForUpdateAsync(Guid deviceId, CancellationToken ct = default);

    Task<DeviceState?> GetStateAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Loads <c>device_states</c> with PostgreSQL <c>FOR UPDATE</c> row lock inside the ambient transaction.
    /// Serializes concurrent inbound writers for the same device so stale sequences cannot overwrite newer state.
    /// </summary>
    Task<DeviceState?> GetStateForUpdateAsync(Guid deviceId, CancellationToken ct = default);

    Task<DeviceSettings?> GetSettingsAsync(Guid deviceId, CancellationToken ct = default);

    Task<DeviceOwnership?> GetOpenOwnershipAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Loads the open ownership row with <c>FOR UPDATE</c> (serializes with transfer/command writers).
    /// </summary>
    Task<DeviceOwnership?> GetOpenOwnershipForUpdateAsync(Guid deviceId, CancellationToken ct = default);

    Task<bool> HasOpenOwnershipAsync(Guid deviceId, CancellationToken ct = default);

    Task<bool> HasActiveCredentialAsync(Guid deviceId, CancellationToken ct = default);

    Task<IReadOnlyList<DeviceCredential>> ListCredentialsAsync(Guid deviceId, CancellationToken ct = default);

    void AddOwnership(DeviceOwnership ownership);

    void AddOutboxMessage(OutboxMessage message);

    Task<IReadOnlyList<OwnerDeviceRow>> ListOpenOwnershipDevicesForOwnerAsync(
        Guid ownerUserId,
        CancellationToken ct = default);

    Task<OwnerDeviceRow?> GetOpenOwnershipDeviceForOwnerAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default);
}

/// <summary>
/// Projection used by the owner device list/detail (settings required for computed connectivity).
/// </summary>
public sealed record OwnerDeviceRow(
    Device Device,
    DeviceOwnership Ownership,
    DeviceState State,
    DeviceSettings Settings);
