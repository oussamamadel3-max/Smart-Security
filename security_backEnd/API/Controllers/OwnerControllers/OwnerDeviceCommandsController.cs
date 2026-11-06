using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using Application.Dtos.OwnerDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.OwnerControllers;

/// <summary>
/// Owner device commands. HTTP 202 + PENDING never means the device executed the command (FR-010…FR-018).
/// </summary>
[ApiController]
[Authorize(Roles = DbStatusCodes.RoleCodes.Owner)]
[Route("api/owner/devices/{deviceId:guid}/commands")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class OwnerDeviceCommandsController : ControllerBase
{
    private readonly IOwnerDeviceCommandService _commands;

    public OwnerDeviceCommandsController(IOwnerDeviceCommandService commands)
    {
        _commands = commands;
    }

    [HttpPost]
    public async Task<IResult> Create(
        Guid deviceId,
        [FromBody] CreateOwnerDeviceCommandRequestDto request,
        CancellationToken ct)
    {
        var created = await _commands.CreateAsync(RequireUserId(), deviceId, request, ct);
        return ApiResponse.Success(
            "تم قبول طلب الأمر وهو قيد التنفيذ بانتظار تأكيد الجهاز.",
            created,
            status: StatusCodes.Status202Accepted);
    }

    [HttpGet("{commandId:guid}")]
    public async Task<IResult> Get(Guid deviceId, Guid commandId, CancellationToken ct)
    {
        var item = await _commands.GetMineAsync(RequireUserId(), deviceId, commandId, ct);
        return ApiResponse.Success("حالة الأمر.", item);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
