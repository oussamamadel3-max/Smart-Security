namespace Application.Dtos.AuthDtos;

public sealed class LoginRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Requested refresh delivery: "cookie" (web) or "body" (mobile).
    /// Not proof of client identity — server still enforces transport rules.
    /// </summary>
    public string RefreshDelivery { get; set; } = "body";
}
