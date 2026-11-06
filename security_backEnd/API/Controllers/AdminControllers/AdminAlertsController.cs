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

/// <summary>
/// Admin alert create/list (FR-026) and device-fault listing (FR-025).
/// FCM delivery remains PENDING until T-09; ownership transfer never cancels ADMIN_ALERT.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class AdminAlertsController : ControllerBase
{
    private readonly IAdminAlertService _alerts;
    private readonly IPermissionGate _gate;

    public AdminAlertsController(IAdminAlertService alerts, IPermissionGate gate)
    {
        _alerts = alerts;
        _gate = gate;
    }

    [HttpPost("alerts")]
    public async Task<IResult> Create([FromBody] CreateAdminAlertRequestDto request, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.AdminAlerts.Manage);
        var dto = await _alerts.CreateAsync(request, RequireUserId(), ct);
        return ApiResponse.Success("تم حفظ التنبيه الإداري.", dto, status: StatusCodes.Status201Created);
    }

    [HttpGet("alerts")]
    public async Task<IResult> List([FromQuery] AdminAlertQueryDto query, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.AdminAlerts.View);
        var page = await _alerts.ListAsync(query, ct);
        return ApiResponse.Success("قائمة التنبيهات الإدارية.", page);
    }

    [HttpGet("alerts/{id:guid}")]
    public async Task<IResult> Get(Guid id, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.AdminAlerts.View);
        var item = await _alerts.GetAsync(id, ct);
        return ApiResponse.Success("تفاصيل التنبيه الإداري.", item);
    }

    /// <summary>
    /// Hardware faults actually reported by devices — not inferred from disconnection.
    /// </summary>
    [HttpGet("device-faults")]
    public async Task<IResult> ListFaults([FromQuery] DeviceFaultAdminQueryDto query, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Devices.View);
        var page = await _alerts.ListFaultsAsync(query, ct);
        return ApiResponse.Success("أعطال العتاد المبلَّغ عنها.", page);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
