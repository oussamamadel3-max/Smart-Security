namespace Application.Interfaces.Services;

/// <summary>
/// Ensures the current actor holds the required effective permissions.
/// </summary>
public interface IPermissionGate
{
    Task EnsureAll(params string[] permissionKeys);
}
