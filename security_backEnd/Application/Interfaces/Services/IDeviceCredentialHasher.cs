namespace Application.Interfaces.Services;

/// <summary>
/// Creates a non-reversible verifier for device PASSWORD_HASH credentials.
/// </summary>
public interface IDeviceCredentialHasher
{
    string HashPassword(string password);
}
