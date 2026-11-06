using Application.Options;
using Domain.Constants;
using Domain.Exceptions;

namespace API.Security;

/// <summary>
/// Resolves refresh delivery channel from transport — rejects Cookie+Body ambiguity.
/// Does not trust clientType / refreshDelivery as app identity.
/// </summary>
public static class RefreshTokenTransport
{
    public const string RefreshDeliveryCookie = "cookie";
    public const string RefreshDeliveryBody = "body";

    public static string ResolveLoginChannel(string? requestedDelivery, HttpRequest request, AuthOptions options)
    {
        var delivery = (requestedDelivery ?? RefreshDeliveryBody).Trim().ToLowerInvariant();
        return delivery switch
        {
            RefreshDeliveryCookie => EnsureCookieChannelAllowed(request, options),
            RefreshDeliveryBody => AuthStatusCodes.RefreshChannel.Body,
            _ => throw new BadRequestException("قيمة refreshDelivery غير مدعومة.", "AUTH_CHANNEL_INVALID")
        };
    }

    public static async Task<(string Channel, string RawToken)> ResolveRefreshPresentationAsync(
        HttpRequest request,
        string? bodyToken,
        AuthOptions options,
        string signingKey,
        Func<string, CancellationToken, Task<Guid?>> resolveSessionIdFromRefreshToken,
        CancellationToken ct = default)
    {
        var cookieName = options.Cookie.Name;
        var hasCookie = request.Cookies.TryGetValue(cookieName, out var cookieToken)
                        && !string.IsNullOrWhiteSpace(cookieToken);
        var hasBody = !string.IsNullOrWhiteSpace(bodyToken);

        if (hasCookie && hasBody)
            throw new BadRequestException("لا يُقبل إرسال Refresh عبر Cookie والجسم معاً.", "AUTH_TOKEN_AMBIGUOUS");

        if (hasCookie)
        {
            EnsureCookieChannelAllowed(request, options);
            var sessionId = await resolveSessionIdFromRefreshToken(cookieToken!, ct)
                ?? throw new UnauthenticatedUserException();
            SignedCsrfToken.EnsureValid(request, options, sessionId, signingKey);
            return (AuthStatusCodes.RefreshChannel.Cookie, cookieToken!);
        }

        if (hasBody)
            return (AuthStatusCodes.RefreshChannel.Body, bodyToken!);

        throw new UnauthenticatedUserException();
    }

    public static string EnsureCookieChannelAllowed(HttpRequest request, AuthOptions options)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin) ||
            options.Cors.AllowedOrigins.Length == 0 ||
            !options.Cors.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Origin غير مسموح لقناة Cookie.", "AUTH_ORIGIN_REJECTED");
        }

        return AuthStatusCodes.RefreshChannel.Cookie;
    }

    public static void AppendRefreshCookie(HttpResponse response, string rawRefresh, AuthOptions options)
    {
        var sameSite = ParseSameSite(options.Cookie.SameSite);
        response.Cookies.Append(options.Cookie.Name, rawRefresh, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Path = options.Cookie.Path,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(options.RefreshTokenDays)
        });
    }

    public static void AppendCsrfCookie(
        HttpResponse response,
        AuthOptions options,
        Guid sessionId,
        string signingKey)
    {
        var value = SignedCsrfToken.Create(sessionId, options, signingKey);
        response.Cookies.Append(options.CsrfCookieName, value, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = options.Cookie.Path,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(options.RefreshTokenDays)
        });
    }

    public static void ClearAuthCookies(HttpResponse response, AuthOptions options)
    {
        response.Cookies.Delete(options.Cookie.Name, new CookieOptions { Path = options.Cookie.Path, Secure = true });
        response.Cookies.Delete(options.CsrfCookieName, new CookieOptions { Path = options.Cookie.Path, Secure = true });
    }

    private static SameSiteMode ParseSameSite(string value)
        => value.Equals("None", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.None
            : value.Equals("Strict", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Strict
            : SameSiteMode.Lax;
}
