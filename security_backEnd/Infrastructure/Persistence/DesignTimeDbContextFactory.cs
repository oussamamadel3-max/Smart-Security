using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

/// <summary>
/// مصنع زمن التصميم لتوليد الترحيلات دون تشغيل الـ API.
/// يقرأ السلسلة من appsettings ثم User Secrets ثم متغيرات البيئة — بلا كلمة مرور مضمّنة.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string ApiUserSecretsId = "esp32-security-backend-api";

    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "API"));
        if (!Directory.Exists(basePath))
            basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "API"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddUserSecrets(typeof(DesignTimeDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var hasPassword = !string.IsNullOrWhiteSpace(connectionString) &&
            (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("Pwd=", StringComparison.OrdinalIgnoreCase));
        if (!hasPassword)
        {
            throw new InvalidOperationException(
                "Design-time connection string is missing credentials. " +
                "Set ConnectionStrings:DefaultConnection via User Secrets " +
                $"('{ApiUserSecretsId}') or environment variable ConnectionStrings__DefaultConnection.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
