using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using Application.Common.Permissions;
using Application.Dtos.AdminDtos;
using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.AdminControllers;

[ApiController]
[Authorize]
[Route("api/admin/devices")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceAdminService _devices;
    private readonly IDeviceOwnershipService _ownerships;
    private readonly IPermissionGate _gate;

    public DevicesController(
        IDeviceAdminService devices,
        IDeviceOwnershipService ownerships,
        IPermissionGate gate)
    {
        _devices = devices;
        _ownerships = ownerships;
        _gate = gate;
    }

    [HttpPost]
    public async Task<IResult> Register([FromBody] RegisterDeviceRequestDto request, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.Manage);
        var dto = await _devices.RegisterAsync(request, RequireUserId(), ct);
        return ApiResponse.Success("تم تسجيل الجهاز.", dto, status: StatusCodes.Status201Created);
    }

    /// <summary>
    /// Initial link only: creates PENDING ownership and queues draft OWNERSHIP_SYNC (FR-004).
    /// </summary>
    [HttpPost("{deviceId:guid}/link")]
    public async Task<IResult> LinkInitial(
        Guid deviceId,
        [FromBody] LinkDeviceRequestDto request,
        CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.Manage);
        var dto = await _ownerships.LinkInitialAsync(deviceId, request, RequireUserId(), ct);
        return ApiResponse.Success("تم إنشاء الربط المعلق.", dto, status: StatusCodes.Status201Created);
    }

    /// <summary>
    /// Transfers open ownership to a new ACTIVE owner in one transaction (FR-006).
    /// Prior-owner pending commands/notifications are cancelled; draft OWNERSHIP_SYNC is queued.
    /// </summary>
    [HttpPost("{deviceId:guid}/transfer")]
    public async Task<IResult> Transfer(
        Guid deviceId,
        [FromBody] TransferDeviceRequestDto request,
        CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.Manage);
        var dto = await _ownerships.TransferAsync(deviceId, request, RequireUserId(), ct);
        return ApiResponse.Success("تم نقل ملكية الجهاز.", dto);
    }

    /// <summary>
    /// Ends open ownership without a new owner and queues draft OWNERSHIP_SYNC (isLinked=false).
    /// </summary>
    [HttpPost("{deviceId:guid}/unlink")]
    public async Task<IResult> Unlink(Guid deviceId, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.Manage);
        var dto = await _ownerships.UnlinkAsync(deviceId, RequireUserId(), ct);
        return ApiResponse.Success("تم إلغاء ربط الجهاز.", dto);
    }

    /// <summary>
    /// Archives a device: RETIRED + end ownership + revoke credentials/broker access (FR-007).
    /// </summary>
    [HttpPost("{deviceId:guid}/archive")]
    public async Task<IResult> Archive(Guid deviceId, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.Manage);
        var dto = await _devices.ArchiveAsync(deviceId, RequireUserId(), ct);
        return ApiResponse.Success("تم أرشفة الجهاز وإبطال وصوله.", dto);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}

