using System.Net;
using System.Text.Json;
using API.IntegrationTests.Support;

namespace API.IntegrationTests;

/// <summary>
/// Reads the Swashbuckle document from the Development test host.
/// Set G1_WRITE_OPENAPI=1 to overwrite docs/contracts/http/openapi.json.
/// A missing database configuration skips the test; it does not invent an OpenAPI file.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class OpenApiDraftExportTests
{
    [SkippableFact]
    public async Task Swagger_Document_Lists_Implemented_Command_Route()
    {
        AuthApiFactory? factory = null;
        try
        {
            factory = new AuthApiFactory();
        }
        catch (InvalidOperationException ex)
        {
            Skip.If(true, ex.Message);
        }

        Assert.NotNull(factory);
        await using var host = factory;
        using var client = host.CreateJsonClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/api/owner/devices/{deviceId}/commands", out var commands));
        Assert.True(commands.TryGetProperty("post", out _));

        if (string.Equals(Environment.GetEnvironmentVariable("G1_WRITE_OPENAPI"), "1", StringComparison.Ordinal))
        {
            var path = OpenApiPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, json);
        }
    }

    private static string OpenApiPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return Path.Combine(dir.FullName, "docs", "contracts", "http", "openapi.json");
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
