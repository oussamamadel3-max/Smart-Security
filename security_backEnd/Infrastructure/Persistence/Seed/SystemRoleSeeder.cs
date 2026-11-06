using Domain.Constants;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent seed of OWNER / ADMIN roles required by the data dictionary.
/// </summary>
public static class SystemRoleSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken ct = default)
    {
        await EnsureRoleAsync(
            db,
            SystemRoleIds.Admin,
            DbStatusCodes.RoleCodes.Admin,
            "مسؤول النظام",
            ct);

        await EnsureRoleAsync(
            db,
            SystemRoleIds.Owner,
            DbStatusCodes.RoleCodes.Owner,
            "مالك جهاز",
            ct);
    }

    private static async Task EnsureRoleAsync(
        AppDbContext db,
        Guid id,
        string code,
        string displayName,
        CancellationToken ct)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(
            r => r.Id == id || r.Name == code,
            ct);

        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.DisplayName))
                existing.DisplayName = displayName;
            if (string.IsNullOrWhiteSpace(existing.NormalizedName))
                existing.NormalizedName = code.ToUpperInvariant();
            await db.SaveChangesAsync(ct);
            return;
        }

        db.Roles.Add(new Role
        {
            Id = id,
            Name = code,
            NormalizedName = code.ToUpperInvariant(),
            DisplayName = displayName,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });

        await db.SaveChangesAsync(ct);
    }
}
