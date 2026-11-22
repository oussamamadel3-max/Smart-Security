using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.Tests;

/// <summary>
/// INT-T-03: validates the shared HTTP OpenAPI draft and JSON samples.
/// Not G1 adoption and not a substitute for live API acceptance.
/// </summary>
public sealed class HttpOpenApiDraftTests
{
    private static readonly string[] RequiredSampleFiles =
    [
        "auth-login-body-success.json",
        "auth-login-cookie-success.json",
        "auth-error-401.json",
        "command-post-request.json",
        "command-post-202-pending.json",
        "command-conflict-409.json",
        "alarm-event-active.json",
        "device-list-item.json"
    ];

    [Fact]
    public void OpenApi_Draft_Is_Valid_Json_With_Expected_Surface()
    {
        var path = OpenApiPath();
        Assert.True(File.Exists(path), path);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        Assert.Equal("3.0.4", root.GetProperty("openapi").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.GetPropertyCount() >= 30);

        Assert.True(paths.TryGetProperty("/api/owner/devices/{deviceId}/commands", out var commands));
        Assert.True(commands.TryGetProperty("post", out var post));
        Assert.True(post.GetProperty("responses").TryGetProperty("202", out _));
        Assert.Equal("implemented", post.GetProperty("x-implementation-status").GetString());

        var schemas = root.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("CreateOwnerDeviceCommandRequestDto", out var createReq));
        Assert.Contains(
            createReq.GetProperty("required").EnumerateArray().Select(x => x.GetString()),
            x => x == "idempotencyKey");
        Assert.True(schemas.TryGetProperty("OwnerDeviceCommandResponseDto", out var cmdResp));
        Assert.True(cmdResp.GetProperty("properties").TryGetProperty("actualStateChanged", out _));
        Assert.True(schemas.TryGetProperty("AuthTokensResponseDto", out _));
        Assert.True(schemas.TryGetProperty("ApiErrorResponse", out _));

        // Created resources must document 201 after INT-T-03 overlay.
        Assert.True(
            paths.GetProperty("/api/admin/owners")
                .GetProperty("post")
                .GetProperty("responses")
                .TryGetProperty("201", out _));
    }

    [Fact]
    public void Proposed_OpenApi_Is_Marked_Proposed_Only()
    {
        var path = ProposedOpenApiPath();
        Assert.True(File.Exists(path), path);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var info = doc.RootElement.GetProperty("info").GetProperty("description").GetString() ?? "";
        Assert.Contains("PROPOSED", info, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT", info, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Http_Samples_Exist_And_Command_Accept_Keeps_ActualStateChanged_False()
    {
        var dir = SamplesDirectory();
        foreach (var name in RequiredSampleFiles)
            Assert.True(File.Exists(Path.Combine(dir, name)), name);

        var pending = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "command-post-202-pending.json")))!;
        Assert.Equal(202, pending["response"]!["status"]!.GetValue<int>());
        var data = pending["response"]!["body"]!["data"]!;
        Assert.Equal("PENDING", data["status"]!.GetValue<string>());
        Assert.False(data["actualStateChanged"]!.GetValue<bool>());

        var request = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "command-post-request.json")))!;
        Assert.False(string.IsNullOrWhiteSpace(request["body"]!["idempotencyKey"]!.GetValue<string>()));

        var bodyLogin = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "auth-login-body-success.json")))!;
        Assert.Equal("body", bodyLogin["request"]!["body"]!["refreshDelivery"]!.GetValue<string>());
        Assert.Equal("body", bodyLogin["response"]!["body"]!["data"]!["refreshChannel"]!.GetValue<string>());

        var cookieLogin = JsonNode.Parse(File.ReadAllText(Path.Combine(dir, "auth-login-cookie-success.json")))!;
        Assert.Equal("cookie", cookieLogin["request"]!["body"]!["refreshDelivery"]!.GetValue<string>());
        Assert.True(cookieLogin["response"]!["body"]!["data"]!["refreshToken"] is null
                    || cookieLogin["response"]!["body"]!["data"]!["refreshToken"]!.GetValueKind() == JsonValueKind.Null);

        // Samples must not embed plausible live secrets.
        var rawPending = File.ReadAllText(Path.Combine(dir, "command-post-202-pending.json"));
        Assert.DoesNotContain("eyJ", rawPending, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenApi_And_Proposed_Do_Not_Duplicate_Implemented_Command_Route_As_Proposed()
    {
        using var implemented = JsonDocument.Parse(File.ReadAllText(OpenApiPath()));
        using var proposed = JsonDocument.Parse(File.ReadAllText(ProposedOpenApiPath()));
        Assert.True(
            implemented.RootElement.GetProperty("paths")
                .TryGetProperty("/api/owner/devices/{deviceId}/commands", out _));
        Assert.False(
            proposed.RootElement.GetProperty("paths")
                .TryGetProperty("/api/owner/devices/{deviceId}/commands", out _));
    }

    private static string OpenApiPath() =>
        Path.Combine(ContractsHttpDirectory(), "openapi.json");

    private static string ProposedOpenApiPath() =>
        Path.Combine(ContractsHttpDirectory(), "openapi.proposed.json");

    private static string SamplesDirectory() =>
        Path.Combine(ContractsHttpDirectory(), "samples");

    private static string ContractsHttpDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "contracts", "http");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("docs/contracts/http was not found.");
    }
}
