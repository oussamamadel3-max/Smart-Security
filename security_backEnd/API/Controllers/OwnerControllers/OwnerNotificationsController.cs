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
/// Owner notification inbox with ownership isolation; ADMIN_ALERT remains recipient-scoped.
/// </summary>
[ApiController]
[Authorize(Roles = DbStatusCodes.RoleCodes.Owner)]
[Route("api/owner/notifications")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class OwnerNotificationsController : ControllerBase
{
    private readonly IOwnerDeviceHistoryService _history;

    public OwnerNotificationsController(IOwnerDeviceHistoryService history)
    {
        _history = history;
    }

    [HttpGet]
    public async Task<IResult> ListMine(CancellationToken ct)
    {
        var items = await _history.ListNotificationsAsync(RequireUserId(), ct);
        return ApiResponse.Success("قائمة الإشعارات.", items);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
