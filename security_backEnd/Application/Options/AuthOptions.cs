namespace Application.Options;

/// <summary>
/// Configurable auth lifetimes and JWT metadata (signing key comes from secrets/env).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public JwtOptions Jwt { get; set; } = new();
    public int AccessTokenMinutes { get; set; } = 10;
    public int RefreshTokenDays { get; set; } = 7;
    public CookieOptions Cookie { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public string CsrfHeaderName { get; set; } = "X-CSRF-TOKEN";
    public string CsrfCookieName { get; set; } = "esp32_csrf";

    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "esp32-security";
        public string Audience { get; set; } = "esp32-security-clients";
        /// <summary>Symmetric signing key — must be supplied via User Secrets / environment.</summary>
        public string SigningKey { get; set; } = string.Empty;
    }

    public sealed class CookieOptions
    {
        // Not __Host- prefixed: Path is scoped to /api/auth (Host cookies require Path=/).
        public string Name { get; set; } = "esp32_rt";
        public string Path { get; set; } = "/api/auth";
        public string SameSite { get; set; } = "Lax";
    }

    public sealed class CorsOptions
    {
        public string[] AllowedOrigins { get; set; } = [];
    }
}
