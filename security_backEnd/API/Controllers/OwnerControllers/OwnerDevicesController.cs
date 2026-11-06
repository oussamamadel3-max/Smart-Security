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
/// Owner device list/detail. PENDING visibility stays limited (FR-035).
/// ACTIVE rows expose independent connection/arm/siren/health indicators (FR-009).
/// </summary>
[ApiController]
[Authorize(Roles = DbStatusCodes.RoleCodes.Owner)]
[Route("api/owner/devices")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class OwnerDevicesController : ControllerBase
{
    private readonly IOwnerDeviceQueryService _devices;

    public OwnerDevicesController(IOwnerDeviceQueryService devices)
    {
        _devices = devices;
    }

    [HttpGet]
    public async Task<IResult> ListMine(CancellationToken ct)
    {
        var items = await _devices.ListMineAsync(RequireUserId(), ct);
        return ApiResponse.Success("قائمة الأجهزة.", items);
    }

    [HttpGet("{deviceId:guid}")]
    public async Task<IResult> GetMine(Guid deviceId, CancellationToken ct)
    {
        var item = await _devices.GetMineAsync(RequireUserId(), deviceId, ct);
        return ApiResponse.Success("تفاصيل الجهاز.", item);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
