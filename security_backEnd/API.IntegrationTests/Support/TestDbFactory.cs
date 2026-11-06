using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace API.IntegrationTests.Support;

/// <summary>
/// اتصال معزول بقاعدة اختبار فقط — لا يمس قواعد الإنتاج.
/// سلسلة الاتصال من ESP32_SECURITY_TEST_DB أو ConnectionStrings__DefaultConnection / User Secrets،
/// مع إجبار اسم قاعدة الاختبار. بلا كلمات مرور مضمّنة في الشيفرة.
/// </summary>
internal static class TestDbFactory
{
    public const string DefaultDatabaseName = "esp32_security_test";

    public static string ResolveConnectionString(string? databaseName = null)
    {
        var dbName = string.IsNullOrWhiteSpace(databaseName) ? DefaultDatabaseName : databaseName;

        var fromEnv = Environment.GetEnvironmentVariable("ESP32_SECURITY_TEST_DB");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return ForceTestDatabase(fromEnv, dbName);

        var fromConfig = TryReadDefaultConnectionFromConfiguration();
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return ForceTestDatabase(fromConfig, dbName);

        throw new InvalidOperationException(
            "Test database connection is not configured. Set ESP32_SECURITY_TEST_DB " +
            "or ConnectionStrings__DefaultConnection / User Secrets " +
            $"({DesignTimeDbContextFactory.ApiUserSecretsId}).");
    }

    private static string ForceTestDatabase(string source, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(source)
        {
            Database = databaseName
        };
        if (string.IsNullOrWhiteSpace(builder.Password))
        {
            throw new InvalidOperationException(
                "Test connection string has no Password. Provide credentials via environment or User Secrets.");
        }

        return builder.ConnectionString;
    }

    private static string? TryReadDefaultConnectionFromConfiguration()
    {
        var apiPathCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "API")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "API")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "API")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "API"))
        };

        var apiPath = apiPathCandidates.FirstOrDefault(Directory.Exists);
        var builder = new ConfigurationBuilder().AddEnvironmentVariables();

        if (apiPath is not null)
        {
            builder
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .AddUserSecrets(typeof(DesignTimeDbContextFactory).Assembly, optional: true);
        }

        return builder.Build().GetConnectionString("DefaultConnection");
    }

    public static async Task<(AppDbContext? Db, string? SkipReason)> CreateMigratedAsync(
        bool wipe = true,
        string? databaseName = null)
    {
        string cs;
        try
        {
            cs = ResolveConnectionString(databaseName);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }

        try
        {
            await EnsureTestDatabaseExistsAsync(cs);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .Options;

            var db = new AppDbContext(options);
            if (wipe)
                await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            return (db, null);
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException or IOException)
        {
            return (null, $"تعذر الاتصال بقاعدة الاختبار: {ex.Message}. عيّن ESP32_SECURITY_TEST_DB.");
        }
    }

    private static async Task EnsureTestDatabaseExistsAsync(string cs)
    {
        var builder = new NpgsqlConnectionStringBuilder(cs);
        var dbName = builder.Database;
        if (string.IsNullOrWhiteSpace(dbName) ||
            !dbName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "رفض الاتصال: اسم قاعدة الاختبار يجب أن يحتوي على 'test' لحماية البيئات غير التجريبية.");
        }

        builder.Database = "postgres";
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @n";
        cmd.Parameters.AddWithValue("n", dbName);
        var exists = await cmd.ExecuteScalarAsync() is not null;
        if (!exists)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = $"CREATE DATABASE \"{dbName.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
