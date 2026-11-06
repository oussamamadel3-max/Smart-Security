using System.Security.Cryptography;
using System.Text;
using Application.Options;
using Domain.Exceptions;

namespace API.Security;

/// <summary>
/// HMAC-signed CSRF token bound to auth session id.
/// Double-submit still requires Cookie == Header, but forged/injected values fail signature checks.
/// </summary>
public static class SignedCsrfToken
{
    private const string Purpose = "esp32-csrf-v1";

    public static string Create(Guid sessionId, AuthOptions options, string signingKey)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var exp = DateTimeOffset.UtcNow.AddDays(Math.Max(1, options.RefreshTokenDays)).ToUnixTimeSeconds();
        var payload = $"{sessionId:N}.{nonce}.{exp}";
        var sig = Sign(signingKey, payload);
        return $"{payload}.{sig}";
    }

    public static void EnsureValid(
        HttpRequest request,
        AuthOptions options,
        Guid sessionId,
        string signingKey)
    {
        if (!request.Cookies.TryGetValue(options.CsrfCookieName, out var cookieVal) ||
            string.IsNullOrWhiteSpace(cookieVal))
            throw new UnauthenticatedUserException();

        if (!request.Headers.TryGetValue(options.CsrfHeaderName, out var headerVals))
            throw new UnauthenticatedUserException();

        var headerVal = headerVals.ToString();
        if (!string.Equals(cookieVal, headerVal, StringComparison.Ordinal))
            throw new UnauthenticatedUserException();

        if (!TryParse(cookieVal, out var tokenSessionId, out var expUnix, out var payload, out var sig))
            throw new UnauthenticatedUserException();

        if (tokenSessionId != sessionId)
            throw new UnauthenticatedUserException();

        if (expUnix < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            throw new UnauthenticatedUserException();

        var expected = Sign(signingKey, payload);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(sig);
        if (expectedBytes.Length != actualBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
            throw new UnauthenticatedUserException();
    }

    private static bool TryParse(
        string token,
        out Guid sessionId,
        out long expUnix,
        out string payload,
        out string sig)
    {
        sessionId = Guid.Empty;
        expUnix = 0;
        payload = string.Empty;
        sig = string.Empty;

        var parts = token.Split('.', 4);
        if (parts.Length != 4)
            return false;

        if (!Guid.TryParseExact(parts[0], "N", out sessionId))
            return false;
        if (!long.TryParse(parts[2], out expUnix))
            return false;

        payload = $"{parts[0]}.{parts[1]}.{parts[2]}";
        sig = parts[3];
        return sig.Length >= 32;
    }

    private static string Sign(string signingKey, string payload)
    {
        var key = Encoding.UTF8.GetBytes(signingKey);
        var data = Encoding.UTF8.GetBytes($"{Purpose}|{payload}");
        return Convert.ToHexString(HMACSHA256.HashData(key, data));
    }
}
