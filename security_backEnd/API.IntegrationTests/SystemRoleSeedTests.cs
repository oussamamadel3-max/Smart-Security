using API.IntegrationTests.Support;
using Domain.Constants;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests;

/// <summary>
/// Verifies OWNER/ADMIN system roles appear after migrate + seed on an empty database.
/// </summary>
public sealed class SystemRoleSeedTests
{
    [Fact]
    public async Task Empty_Database_After_Migrate_Gets_Owner_And_Admin_Roles()
    {
        // قاعدة اختبار منفصلة حتى لا تُمس بذرة مجموعة PostgresDb المشتركة.
        var (db, skip) = await TestDbFactory.CreateMigratedAsync(
            wipe: true,
            databaseName: "esp32_security_role_seed_test");
        if (skip is not null)
            Assert.Fail(skip);

        await using (db)
        {
            Assert.NotNull(db);

            // Empty of business seed — only schema from migrations.
            Assert.Equal(0, await db.Roles.CountAsync());
            Assert.Equal(0, await db.Users.CountAsync());

            await SystemRoleSeeder.EnsureAsync(db);
            await SystemRoleSeeder.EnsureAsync(db); // idempotent

            var roles = await db.Roles.OrderBy(r => r.Name).ToListAsync();
            Assert.Equal(2, roles.Count);

            var admin = Assert.Single(roles, r => r.Name == DbStatusCodes.RoleCodes.Admin);
            Assert.Equal(SystemRoleIds.Admin, admin.Id);
            Assert.Equal("ADMIN", admin.NormalizedName);
            Assert.False(string.IsNullOrWhiteSpace(admin.DisplayName));

            var owner = Assert.Single(roles, r => r.Name == DbStatusCodes.RoleCodes.Owner);
            Assert.Equal(SystemRoleIds.Owner, owner.Id);
            Assert.Equal("OWNER", owner.NormalizedName);
            Assert.False(string.IsNullOrWhiteSpace(owner.DisplayName));
        }
    }
}
