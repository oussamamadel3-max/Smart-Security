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

