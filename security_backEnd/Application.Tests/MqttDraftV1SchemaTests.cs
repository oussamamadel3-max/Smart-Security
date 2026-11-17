using System.Text.Json.Nodes;
using Json.Schema;

namespace Application.Tests;

/// <summary>
/// Validates the MQTT draft v1 samples against the JSON Schemas extracted from the
/// implemented builders and parsers. This is not G1 adoption.
/// </summary>
public sealed class MqttDraftV1SchemaTests
{
    private static readonly string[] ImplementedSchemas =
    [
        "execute-command",
        "command-ack",
        "state",
        "heartbeat",
        "alarm-event",
        "event-persisted",
        "ownership-sync",
        "event-loss-report",
        "device-fault"
    ];

    public static IEnumerable<object[]> AcceptSamples() =>
        Directory.GetFiles(AcceptDirectory(), "*.json").Select(path => new object[] { path });

    public static IEnumerable<object[]> RejectSamples() =>
        Directory.GetFiles(RejectDirectory(), "*.json").Select(path => new object[] { path });

    [Fact]
    public void Implemented_Message_Types_Have_Schemas_And_Accept_Samples()
    {
        foreach (var name in ImplementedSchemas)
        {
            Assert.True(File.Exists(Path.Combine(SchemaDirectory(), name + ".schema.json")), name);
            Assert.Contains(
                Directory.GetFiles(AcceptDirectory(), name + ".json"),
                path => Path.GetFileName(path) == name + ".json");
        }
    }

    [Fact]
    public void Unresolved_Message_Types_Have_No_Adopted_Schema()
    {
        var dir = SchemaDirectory();
        Assert.False(File.Exists(Path.Combine(dir, "alarm-end.schema.json")));
        Assert.False(File.Exists(Path.Combine(dir, "settings-sync.schema.json")));
    }

    [Theory]
    [MemberData(nameof(AcceptSamples))]
    public void Accept_Sample_Matches_Its_Schema(string samplePath)
    {
        var result = Evaluate(samplePath);
        Assert.True(result.IsValid, samplePath + " failed schema evaluation.");
    }

    [Theory]
    [MemberData(nameof(RejectSamples))]
    public void Reject_Sample_Fails_Its_Schema(string samplePath)
    {
        var result = Evaluate(samplePath);
        Assert.False(result.IsValid, samplePath);
    }

    private static EvaluationResults Evaluate(string samplePath)
    {
        var key = SchemaKey(samplePath);
        var schemaPath = Path.Combine(SchemaDirectory(), key + ".schema.json");
        Assert.True(File.Exists(schemaPath), schemaPath);
        var schema = JsonSchema.FromFile(schemaPath);
        var node = JsonNode.Parse(File.ReadAllText(samplePath));
        return schema.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
    }

    private static string SchemaKey(string samplePath)
    {
        var stem = Path.GetFileNameWithoutExtension(samplePath);
        var split = stem.IndexOf("--", StringComparison.Ordinal);
        return split < 0 ? stem : stem[..split];
    }

    private static string SchemaDirectory() =>
        Path.Combine(RepoRoot(), "docs", "contracts", "mqtt", "v1");

    private static string AcceptDirectory() =>
        Path.Combine(SchemaDirectory(), "samples", "accept");

    private static string RejectDirectory() =>
        Path.Combine(SchemaDirectory(), "samples", "reject");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root (AGENTS.md) was not found from the test output directory.");
    }
}
