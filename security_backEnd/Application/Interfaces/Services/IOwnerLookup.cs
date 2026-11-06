namespace Application.Interfaces.Services;

/// <summary>
/// Read-only ownership checks for admin account lifecycle rules.
/// </summary>
public interface IOwnerLookup
{
    Task<bool> HasOpenDeviceOwnershipAsync(Guid ownerUserId, CancellationToken ct = default);
}
