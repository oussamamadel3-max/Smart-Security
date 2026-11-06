using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using Application.Dtos.PushTokenDtos;
using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Register / list / revoke FCM push tokens for the authenticated account (FCM-03).
/// Tokens are scoped to the caller — never readable across users.
/// </summary>
[ApiController]
[Authorize]
[Route("api/me/push-tokens")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class PushTokensController : ControllerBase
{
    private readonly IPushTokenService _pushTokens;

    public PushTokensController(IPushTokenService pushTokens)
    {
        _pushTokens = pushTokens;
    }

    [HttpGet]
    public async Task<IResult> ListMine(CancellationToken ct)
    {
        var items = await _pushTokens.ListMineAsync(RequireUserId(), ct);
        return ApiResponse.Success("رموز الدفع الخاصة بك.", items);
    }

    [HttpPost]
    public async Task<IResult> RegisterOrUpdate(
        [FromBody] RegisterPushTokenRequestDto request,
        CancellationToken ct)
    {
        var item = await _pushTokens.RegisterOrUpdateAsync(RequireUserId(), request, ct);
        return ApiResponse.Success("تم تسجيل رمز الدفع.", item);
    }

    [HttpDelete("{pushTokenId:guid}")]
    public async Task<IResult> Revoke(Guid pushTokenId, CancellationToken ct)
    {
        await _pushTokens.RevokeAsync(RequireUserId(), pushTokenId, ct);
        return ApiResponse.Success("تم إبطال رمز الدفع.", (object?)null);
    }

    [HttpDelete]
    public async Task<IResult> RevokeAll(CancellationToken ct)
    {
        await _pushTokens.RevokeAllActiveAsync(RequireUserId(), ct);
        return ApiResponse.Success("تم إبطال جميع رموز الدفع النشطة.", (object?)null);
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
