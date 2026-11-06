using Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

/// <summary>
/// Uses ASP.NET Identity's PasswordHasher so MQTT password verifiers are never stored in plaintext.
/// </summary>
public sealed class DeviceCredentialHasher : IDeviceCredentialHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string HashPassword(string password)
        => _hasher.HashPassword(new object(), password);
}
