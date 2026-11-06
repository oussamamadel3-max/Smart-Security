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

    /// <summary>
    /// Opens notification detail with a fresh ownership check (FCM-05).
    /// FCM ACCEPTED alone never proves the user may still view the source.
    /// </summary>
    [HttpGet("{notificationId:guid}")]
    public async Task<IResult> GetDetail(Guid notificationId, CancellationToken ct)
    {
        var item = await _history.GetNotificationDetailAsync(RequireUserId(), notificationId, ct);
        return ApiResponse.Success("تفاصيل الإشعار.", item);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
