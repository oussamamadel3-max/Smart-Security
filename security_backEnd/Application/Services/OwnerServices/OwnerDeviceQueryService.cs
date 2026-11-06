using Application.Dtos.OwnerDtos;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;

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
        return rows.Select(Map).ToList();
    }

    private static OwnerDeviceListItemDto Map(OwnerDeviceRow row)
    {
        var isPending = row.Ownership.Status == DbStatusCodes.Ownership.Pending;
        var synced = row.State.ConfirmedOwnershipVersion >= row.Ownership.Version;
        var canControl = !isPending
                         && row.Ownership.Status == DbStatusCodes.Ownership.Active
                         && synced;

        return new OwnerDeviceListItemDto
        {
            DeviceId = row.Device.Id,
            DisplayName = row.Device.DisplayName,
            OwnershipStatus = row.Ownership.Status,
            OwnershipVersion = row.Ownership.Version,
            ConfirmedOwnershipVersion = row.State.ConfirmedOwnershipVersion,
            AwaitingOwnershipAck = isPending || !synced,
            CanControl = canControl,
            LinkStatusMessage = isPending
                ? "الربط قيد الاكتمال"
                : (synced ? "الملكية فعالة" : "بانتظار مزامنة إصدار الملكية")
        };
    }
}
