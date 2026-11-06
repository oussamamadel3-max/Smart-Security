using API.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.IntegrationTests;

/// <summary>
/// المرحلة 1: مراجعة البنية الفعلية مقابل التصميم المعتمد لنظام الأمان الذكي.
/// </summary>
[Collection("PostgresDb")]
public sealed class DatabaseSchemaReviewTests
{
    private readonly PostgresDbFixture _fx;

    public DatabaseSchemaReviewTests(PostgresDbFixture fx) => _fx = fx;

    private static readonly string[] ExpectedTables =
    [
        "users", "roles", "user_roles", "user_claims", "user_logins", "user_tokens", "role_claims",
        "auth_sessions", "auth_refresh_token_history",
        "devices", "device_ownerships", "device_credentials", "device_sensors", "device_settings", "device_states",
        "device_commands", "command_status_history",
        "alarm_events", "device_faults", "event_loss_reports",
        "push_tokens", "admin_alerts", "notifications", "notification_deliveries",
        "audit_logs", "outbox_messages"
    ];

    [Fact]
    public async Task All_Design_Tables_Exist()
    {
        _fx.RequireDbOrFail();
        var names = await QueryStringsAsync(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY 1
            """);

        foreach (var table in ExpectedTables)
            Assert.Contains(table, names);

        // لا توجد جداول فروع/خدمات — خارج نطاق مخطط ESP32
        Assert.DoesNotContain("branches", names);
        Assert.DoesNotContain("services", names);
    }

    [Fact]
    public async Task ForeignKeys_Are_Restrict_Not_Cascade()
    {
        _fx.RequireDbOrFail();
        var cascading = await QueryStringsAsync(
            """
            SELECT c.conname
            FROM pg_constraint c
            JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE c.contype = 'f'
              AND n.nspname = 'public'
              AND pg_get_constraintdef(c.oid) ILIKE '%ON DELETE CASCADE%'
            """);
        Assert.Empty(cascading);
    }

    [Fact]
    public async Task Check_Constraints_Cover_Critical_Rules()
    {
        _fx.RequireDbOrFail();
        var checks = await QueryStringsAsync(
            """
            SELECT c.conname
            FROM pg_constraint c
            JOIN pg_namespace n ON n.oid = c.connamespace
            WHERE c.contype = 'c' AND n.nspname = 'public'
            ORDER BY 1
            """);

        Assert.Contains("ck_users_username_not_blank", checks);
        Assert.Contains("ck_users_full_name_not_blank", checks);
        Assert.Contains("ck_users_deleted_implies_inactive", checks);
        Assert.Contains("ck_devices_lifecycle_status", checks);
        Assert.Contains("ck_device_ownerships_status", checks);
        Assert.Contains("ck_device_commands_status", checks);
        Assert.Contains("ck_notifications_single_source", checks);
        Assert.True(checks.Count >= 40, $"Expected many CHECKs, got {checks.Count}");
    }

    [Fact]
    public async Task SoftDelete_Unique_Indexes_Exist_With_Filter()
    {
        _fx.RequireDbOrFail();
        var indexes = await QueryAsync(
            """
            SELECT indexname AS name, indexdef AS def
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname IN ('UserNameIndex', 'ux_users_username_lower', 'ux_users_email_lower')
            """);

        Assert.Equal(3, indexes.Count);
        foreach (var row in indexes)
            Assert.Contains("deleted_at IS NULL", row["def"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Applied_Migrations_Include_Initial_And_SoftDelete()
    {
        _fx.RequireDbOrFail();
        var applied = (await _fx.Db!.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains(applied, m => m.Contains("InitialCreate", StringComparison.Ordinal));
        Assert.Contains(applied, m => m.Contains("UsersFullNameCheckAndSoftDeleteUniqueness", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Schema_Inventory_Counts_Are_Documentable()
    {
        _fx.RequireDbOrFail();
        var tables = await ScalarIntAsync("SELECT count(*)::int FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE'");
        var fks = await ScalarIntAsync("SELECT count(*)::int FROM pg_constraint c JOIN pg_namespace n ON n.oid=c.connamespace WHERE c.contype='f' AND n.nspname='public'");
        var checks = await ScalarIntAsync("SELECT count(*)::int FROM pg_constraint c JOIN pg_namespace n ON n.oid=c.connamespace WHERE c.contype='c' AND n.nspname='public'");
        var indexes = await ScalarIntAsync("SELECT count(*)::int FROM pg_indexes WHERE schemaname='public'");
        var columns = await ScalarIntAsync("SELECT count(*)::int FROM information_schema.columns WHERE table_schema='public'");

        // تخزين مؤقت للقراءة في التقرير عبر Assert messages عند الفشل؛ هنا نؤكد الحدود الدنيا.
        Assert.True(tables >= ExpectedTables.Length);
        Assert.True(fks >= 30);
        Assert.True(checks >= 40);
        Assert.True(indexes >= 40);
        Assert.True(columns >= 150);

        // اكتب ملفاً جانبياً للتقرير (مجلد docs عبر مسار نسبي من مخرجات الاختبار غير مضمون) — نستخدم خاصية ثابتة عبر Assert.True message
        SchemaInventoryCapture.Last = new SchemaInventoryCapture.Snapshot(tables, columns, fks, checks, indexes, ExpectedTables.Length);
    }

    private async Task<List<string>> QueryStringsAsync(string sql)
    {
        var rows = await QueryAsync(sql);
        return rows.Select(r => (string)r.Values.First()!).ToList();
    }

    private async Task<List<Dictionary<string, string>>> QueryAsync(string sql)
    {
        var list = new List<Dictionary<string, string>>();
        var conn = (NpgsqlConnection)_fx.Db!.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString() ?? "";
            list.Add(row);
        }
        return list;
    }

    private async Task<int> ScalarIntAsync(string sql)
    {
        var conn = (NpgsqlConnection)_fx.Db!.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}

internal static class SchemaInventoryCapture
{
    internal sealed record Snapshot(int Tables, int Columns, int ForeignKeys, int Checks, int Indexes, int ExpectedDesignTables);
    internal static Snapshot? Last { get; set; }
}
