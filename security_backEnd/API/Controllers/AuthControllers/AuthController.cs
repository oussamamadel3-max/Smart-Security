using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using API.Security;
using Application.Dtos.AuthDtos;
using Application.Interfaces.Services;
using Application.Options;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers.AuthControllers;

[ApiController]
[Route("api/auth")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AuthOptions _options;

    public AuthController(IAuthService authService, IOptions<AuthOptions> options)
    {
        _authService = authService;
        _options = options.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var channel = RefreshTokenTransport.ResolveLoginChannel(request.RefreshDelivery, Request, _options);
        var tokens = await _authService.LoginAsync(request, channel, ct);
        ApplyRefreshTransport(tokens);
        return ApiResponse.Success("تم تسجيل الدخول بنجاح.", tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IResult> Refresh([FromBody] RefreshRequestDto? request, CancellationToken ct)
    {
        var (channel, raw) = await ResolveRefreshAsync(request?.RefreshToken, ct);
        var tokens = await _authService.RefreshAsync(raw, channel, ct);
        ApplyRefreshTransport(tokens);
        return ApiResponse.Success("تم تجديد الجلسة.", tokens);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IResult> Logout([FromBody] RefreshRequestDto? request, CancellationToken ct)
    {
        var userId = RequireUserId();
        var sessionId = RequireSessionId();
        string? raw = null;
        try
        {
            var resolved = await ResolveRefreshAsync(request?.RefreshToken, ct);
            raw = resolved.RawToken;
        }
        catch (UnauthenticatedUserException)
        {
            // Access-only logout still revokes by sid.
        }
        catch (BadRequestException)
        {
            // Ambiguous transport — still revoke by sid.
        }

        await _authService.LogoutAsync(userId, sessionId, raw, ct);
        RefreshTokenTransport.ClearAuthCookies(Response, _options);
        return ApiResponse.Success("تم تسجيل الخروج.");
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IResult> LogoutAll(CancellationToken ct)
    {
        var userId = RequireUserId();
        await _authService.LogoutAllAsync(userId, ct);
        RefreshTokenTransport.ClearAuthCookies(Response, _options);
        return ApiResponse.Success("تم إنهاء جميع الجلسات.");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IResult> Me(CancellationToken ct)
    {
        var me = await _authService.GetMeAsync(RequireUserId(), ct);
        return ApiResponse.Success("تم جلب الملف الشخصي بنجاح.", me);
    }

    private Task<(string Channel, string RawToken)> ResolveRefreshAsync(string? bodyToken, CancellationToken ct)
        => RefreshTokenTransport.ResolveRefreshPresentationAsync(
            Request,
            bodyToken,
            _options,
            _options.Jwt.SigningKey,
            (raw, token) => _authService.FindActiveSessionIdByRefreshTokenAsync(raw, token),
            ct);

    private void ApplyRefreshTransport(AuthTokensResponseDto tokens)
    {
        if (tokens.RefreshChannel == AuthStatusCodes.RefreshChannel.Cookie)
        {
            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
                throw new InvalidOperationException("Cookie channel requires refresh token material.");

            RefreshTokenTransport.AppendRefreshCookie(Response, tokens.RefreshToken, _options);
            RefreshTokenTransport.AppendCsrfCookie(Response, _options, tokens.SessionId, _options.Jwt.SigningKey);
            tokens.RefreshToken = null;
        }
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }

    private Guid RequireSessionId()
    {
        var raw = User.FindFirstValue("sid");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
