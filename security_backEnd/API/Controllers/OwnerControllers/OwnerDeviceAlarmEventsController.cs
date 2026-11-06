using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.OwnerControllers;

/// <summary>
/// Owner alarm-event history scoped to the caller's current ownership period (FR-005 / FR-032).
/// </summary>
[ApiController]
[Authorize(Roles = DbStatusCodes.RoleCodes.Owner)]
[Route("api/owner/devices/{deviceId:guid}/alarm-events")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class OwnerDeviceAlarmEventsController : ControllerBase
{
    private readonly IOwnerDeviceHistoryService _history;

    public OwnerDeviceAlarmEventsController(IOwnerDeviceHistoryService history)
    {
        _history = history;
    }

    [HttpGet]
    public async Task<IResult> List(Guid deviceId, CancellationToken ct)
    {
        var items = await _history.ListAlarmEventsAsync(RequireUserId(), deviceId, ct);
        return ApiResponse.Success("سجل حوادث فترة الملكية الحالية.", items);
    }

    [HttpGet("{alarmEventId:guid}")]
    public async Task<IResult> Get(Guid deviceId, Guid alarmEventId, CancellationToken ct)
    {
        var item = await _history.GetAlarmEventAsync(RequireUserId(), deviceId, alarmEventId, ct);
        return ApiResponse.Success("تفاصيل الحادثة.", item);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
