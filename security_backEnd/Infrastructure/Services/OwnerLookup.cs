using Application.Interfaces.Services;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class OwnerLookup : IOwnerLookup
{
    private readonly AppDbContext _db;

    public OwnerLookup(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasOpenDeviceOwnershipAsync(Guid ownerUserId, CancellationToken ct = default)
        => _db.DeviceOwnerships.AnyAsync(
            o => o.OwnerUserId == ownerUserId &&
                 o.EndedAt == null &&
                 (o.Status == DbStatusCodes.Ownership.Pending || o.Status == DbStatusCodes.Ownership.Active),
            ct);
}
