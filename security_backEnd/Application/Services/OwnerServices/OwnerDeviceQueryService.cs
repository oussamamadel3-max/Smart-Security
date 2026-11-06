using Application.Common.Devices;
using Application.Dtos.OwnerDtos;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Exceptions;

namespace Application.Services.OwnerServices;

public sealed class OwnerDeviceQueryService : IOwnerDeviceQueryService
{
    private readonly IDeviceRepository _devices;

    public OwnerDeviceQueryService(IDeviceRepository devices)
    {
        _devices = devices;
    }

    public async Task<IReadOnlyList<OwnerDeviceListItemDto>> ListMineAsync(
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        var rows = await _devices.ListOpenOwnershipDevicesForOwnerAsync(ownerUserId, ct);
        var now = DateTimeOffset.UtcNow;
        return rows.Select(row => MapListItem(row, now)).ToList();
    }

    public async Task<OwnerDeviceDetailDto> GetMineAsync(
        Guid ownerUserId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var row = await _devices.GetOpenOwnershipDeviceForOwnerAsync(ownerUserId, deviceId, ct)
            ?? throw new NotFoundException("الجهاز غير موجود ضمن ملكيتك.", "DEVICE_NOT_FOUND");

        return MapDetail(row, DateTimeOffset.UtcNow);
    }

    private static OwnerDeviceListItemDto MapListItem(OwnerDeviceRow row, DateTimeOffset utcNow)
    {
        var link = MapLinkFields(row);
        return new OwnerDeviceListItemDto
        {
            DeviceId = row.Device.Id,
            DisplayName = row.Device.DisplayName,
            OwnershipStatus = link.OwnershipStatus,
            OwnershipVersion = link.OwnershipVersion,
            ConfirmedOwnershipVersion = link.ConfirmedOwnershipVersion,
            AwaitingOwnershipAck = link.AwaitingOwnershipAck,
            CanControl = link.CanControl,
            LinkStatusMessage = link.LinkStatusMessage,
            Indicators = link.IncludeIndicators ? MapIndicators(row, utcNow) : null
        };
    }

    private static OwnerDeviceDetailDto MapDetail(OwnerDeviceRow row, DateTimeOffset utcNow)
    {
        var link = MapLinkFields(row);
        return new OwnerDeviceDetailDto
        {
            DeviceId = row.Device.Id,
            DisplayName = row.Device.DisplayName,
            OwnershipStatus = link.OwnershipStatus,
            OwnershipVersion = link.OwnershipVersion,
            ConfirmedOwnershipVersion = link.ConfirmedOwnershipVersion,
            AwaitingOwnershipAck = link.AwaitingOwnershipAck,
            CanControl = link.CanControl,
            LinkStatusMessage = link.LinkStatusMessage,
            DeviceLifecycleStatus = row.Device.LifecycleStatus,
            Indicators = link.IncludeIndicators ? MapIndicators(row, utcNow) : null
        };
    }

    private static LinkProjection MapLinkFields(OwnerDeviceRow row)
    {
        var isPending = row.Ownership.Status == DbStatusCodes.Ownership.Pending;
        var synced = row.State.ConfirmedOwnershipVersion >= row.Ownership.Version;
        var isActive = !isPending && row.Ownership.Status == DbStatusCodes.Ownership.Active;
        var canControl = isActive && synced;

        return new LinkProjection(
            OwnershipStatus: row.Ownership.Status,
            OwnershipVersion: row.Ownership.Version,
            ConfirmedOwnershipVersion: row.State.ConfirmedOwnershipVersion,
            AwaitingOwnershipAck: isPending || !synced,
            CanControl: canControl,
            LinkStatusMessage: isPending
                ? "الربط قيد الاكتمال"
                : (synced ? "الملكية فعالة" : "بانتظار مزامنة إصدار الملكية"),
            // Operational indicators only when ownership is ACTIVE (FR-035 limited PENDING view).
            IncludeIndicators: isActive);
    }

    private static OwnerDeviceIndicatorsDto MapIndicators(OwnerDeviceRow row, DateTimeOffset utcNow)
    {
        // Never invent HEALTHY: persisted health_state defaults to UNKNOWN until the device reports otherwise.
        var connection = DeviceConnectivity.ComputeStatus(
            row.State.LastSeenAt,
            row.Settings.HeartbeatTimeoutSeconds,
            utcNow);

        return new OwnerDeviceIndicatorsDto
        {
            ConnectionStatus = connection,
            ArmState = row.State.ArmState,
            SirenState = row.State.SirenState,
            HealthState = row.State.HealthState,
            HealthCode = row.State.HealthCode,
            LastSeenAt = row.State.LastSeenAt,
            LastStateAt = row.State.LastStateAt,
            StateSequence = row.State.StateSequence,
            ClockTrusted = row.State.ClockTrusted,
            CurrentAlarmEventUid = row.State.CurrentAlarmEventUid,
            PowerSource = row.State.PowerSource,
            BatteryPercent = row.State.BatteryPercent,
            HeartbeatTimeoutSeconds = row.Settings.HeartbeatTimeoutSeconds
        };
    }

    private sealed record LinkProjection(
        string OwnershipStatus,
        long OwnershipVersion,
        long ConfirmedOwnershipVersion,
        bool AwaitingOwnershipAck,
        bool CanControl,
        string LinkStatusMessage,
        bool IncludeIndicators);
}
