using Application.Interfaces.Repositories;
using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Entities.Operations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _db;

    public DeviceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByHardwareUidAsync(string hardwareUid, CancellationToken ct = default)
        => _db.Devices.AnyAsync(d => d.HardwareUid == hardwareUid, ct);

    public void AddRegistrationGraph(
        Device device,
        DeviceState state,
        DeviceSettings settings,
        DeviceCredential credential)
    {
        _db.Devices.Add(device);
        _db.DeviceStates.Add(state);
        _db.DeviceSettings.Add(settings);
        _db.DeviceCredentials.Add(credential);
    }

    public Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken ct = default)
        => _db.Devices.SingleOrDefaultAsync(d => d.Id == deviceId, ct);

    public async Task<Device?> GetByIdForUpdateAsync(Guid deviceId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM devices WHERE id = {deviceId} FOR UPDATE",
            ct);

        return await _db.Devices.SingleOrDefaultAsync(d => d.Id == deviceId, ct);
    }

    public Task<DeviceState?> GetStateAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceStates.SingleOrDefaultAsync(s => s.DeviceId == deviceId, ct);

    public async Task<DeviceState?> GetStateForUpdateAsync(Guid deviceId, CancellationToken ct = default)
    {
        // Acquire a row-level lock before EF tracks the entity so concurrent STATE/ACK writers serialize.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM device_states WHERE device_id = {deviceId} FOR UPDATE",
            ct);

        return await _db.DeviceStates.SingleOrDefaultAsync(s => s.DeviceId == deviceId, ct);
    }

    public Task<DeviceSettings?> GetSettingsAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceSettings.SingleOrDefaultAsync(s => s.DeviceId == deviceId, ct);

    public Task<DeviceOwnership?> GetOpenOwnershipAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceOwnerships.SingleOrDefaultAsync(
            o => o.DeviceId == deviceId && o.EndedAt == null,
            ct);

    public async Task<DeviceOwnership?> GetOpenOwnershipForUpdateAsync(
        Guid deviceId,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1 FROM device_ownerships
             WHERE device_id = {deviceId} AND ended_at IS NULL
             FOR UPDATE
             """,
            ct);

        return await _db.DeviceOwnerships.SingleOrDefaultAsync(
            o => o.DeviceId == deviceId && o.EndedAt == null,
            ct);
    }

    public Task<bool> HasOpenOwnershipAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceOwnerships.AnyAsync(o => o.DeviceId == deviceId && o.EndedAt == null, ct);

    public Task<bool> HasActiveCredentialAsync(Guid deviceId, CancellationToken ct = default)
        => _db.DeviceCredentials.AnyAsync(
            c => c.DeviceId == deviceId && c.RevokedAt == null,
            ct);

    public async Task<IReadOnlyList<DeviceCredential>> ListCredentialsAsync(
        Guid deviceId,
        CancellationToken ct = default)
    {
        return await _db.DeviceCredentials
            .Where(c => c.DeviceId == deviceId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public void AddOwnership(DeviceOwnership ownership)
        => _db.DeviceOwnerships.Add(ownership);

    public void AddOutboxMessage(OutboxMessage message)
        => _db.OutboxMessages.Add(message);

    public async Task<IReadOnlyList<OwnerDeviceRow>> ListOpenOwnershipDevicesForOwnerAsync(
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        var rows = await (
            from ownership in _db.DeviceOwnerships.AsNoTracking()
            join device in _db.Devices.AsNoTracking() on ownership.DeviceId equals device.Id
            join state in _db.DeviceStates.AsNoTracking() on device.Id equals state.DeviceId
            join settings in _db.DeviceSettings.AsNoTracking() on device.Id equals settings.DeviceId
            where ownership.OwnerUserId == ownerUserId
                  && ownership.EndedAt == null
                  && device.LifecycleStatus != DbStatusCodes.DeviceLifecycle.Retired
                  && (ownership.Status == DbStatusCodes.Ownership.Pending
                      || ownership.Status == DbStatusCodes.Ownership.Active)
            orderby ownership.StartedAt descending
            select new OwnerDeviceRow(device, ownership, state, settings)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<OwnerDeviceRow?> GetOpenOwnershipDeviceForOwnerAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var row = await (
            from ownership in _db.DeviceOwnerships.AsNoTracking()
            join device in _db.Devices.AsNoTracking() on ownership.DeviceId equals device.Id
            join state in _db.DeviceStates.AsNoTracking() on device.Id equals state.DeviceId
            join settings in _db.DeviceSettings.AsNoTracking() on device.Id equals settings.DeviceId
            where ownership.OwnerUserId == ownerUserId
                  && ownership.DeviceId == deviceId
                  && ownership.EndedAt == null
                  && device.LifecycleStatus != DbStatusCodes.DeviceLifecycle.Retired
                  && (ownership.Status == DbStatusCodes.Ownership.Pending
                      || ownership.Status == DbStatusCodes.Ownership.Active)
            select new OwnerDeviceRow(device, ownership, state, settings)
        ).SingleOrDefaultAsync(ct);

        return row;
    }

    public async Task<IReadOnlyList<AdminDeviceRow>> ListAdminDevicesAsync(CancellationToken ct = default)
    {
        var rows = await (
            from device in _db.Devices.AsNoTracking()
            join state in _db.DeviceStates.AsNoTracking() on device.Id equals state.DeviceId
            join settings in _db.DeviceSettings.AsNoTracking() on device.Id equals settings.DeviceId
            join ownership in _db.DeviceOwnerships.AsNoTracking().Where(o => o.EndedAt == null)
                on device.Id equals ownership.DeviceId into ownershipJoin
            from ownership in ownershipJoin.DefaultIfEmpty()
            join owner in _db.Users.AsNoTracking()
                on ownership.OwnerUserId equals owner.Id into ownerJoin
            from owner in ownerJoin.DefaultIfEmpty()
            orderby device.CreatedAt descending
            select new AdminDeviceRow(device, state, settings, ownership, owner)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<AdminDeviceRow?> GetAdminDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        var row = await (
            from device in _db.Devices.AsNoTracking()
            join state in _db.DeviceStates.AsNoTracking() on device.Id equals state.DeviceId
            join settings in _db.DeviceSettings.AsNoTracking() on device.Id equals settings.DeviceId
            where device.Id == deviceId
            join ownership in _db.DeviceOwnerships.AsNoTracking().Where(o => o.EndedAt == null)
                on device.Id equals ownership.DeviceId into ownershipJoin
            from ownership in ownershipJoin.DefaultIfEmpty()
            join owner in _db.Users.AsNoTracking()
                on ownership.OwnerUserId equals owner.Id into ownerJoin
            from owner in ownerJoin.DefaultIfEmpty()
            select new AdminDeviceRow(device, state, settings, ownership, owner)
        ).SingleOrDefaultAsync(ct);

        return row;
    }
}
