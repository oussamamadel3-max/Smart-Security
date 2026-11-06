using Infrastructure.Persistence;

namespace API.IntegrationTests.Support;

/// <summary>
/// قاعدة اختبار واحدة لكل مجموعة: ترحيل + بذرة سيناريو مترابطة.
/// </summary>
public sealed class PostgresDbFixture : IAsyncLifetime
{
    public AppDbContext? Db { get; private set; }
    public string? SkipReason { get; private set; }
    public DatabaseScenarioSeed.Counts? SeedCounts { get; private set; }

    public async Task InitializeAsync()
    {
        var (db, skip) = await TestDbFactory.CreateMigratedAsync(wipe: true);
        SkipReason = skip;
        Db = db;
        if (db is null)
            return;

        SeedCounts = await DatabaseScenarioSeed.ApplyAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (Db is not null)
            await Db.DisposeAsync();
    }

    public bool TryRequireDb()
    {
        if (SkipReason is not null)
            return false;
        Assert.NotNull(Db);
        return true;
    }

    public void RequireDbOrFail()
    {
        if (SkipReason is not null)
            Assert.Fail(SkipReason);
        Assert.NotNull(Db);
    }
}

[CollectionDefinition("PostgresDb")]
public sealed class PostgresDbCollection : ICollectionFixture<PostgresDbFixture>
{
}
