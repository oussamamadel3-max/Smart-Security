using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace API.IntegrationTests;

[CollectionDefinition(nameof(IsolatedMosquittoAclTests), DisableParallelization = true)]
public sealed class IsolatedMosquittoAclCollection;

/// <summary>
/// Starts a disposable Mosquitto container with per-device ACL.
/// The password is generated in memory and is not written to the repository.
/// Absence of Docker is a skip, not a pass.
/// </summary>
[Collection(nameof(IsolatedMosquittoAclTests))]
public sealed class IsolatedMosquittoAclTests : IAsyncLifetime
{
    private const string DeviceA = "11111111-1111-4111-8111-111111111111";
    private const string DeviceB = "22222222-2222-4222-8222-222222222222";

    private string? _containerName;
    private string? _tempDir;
    private int _port;
    private string? _password;
    private bool _dockerReady;
    private string _dockerDetail = "Docker was not probed.";

    public async Task InitializeAsync()
    {
        (int ExitCode, string StdOut, string StdErr) docker;
        try
        {
            docker = await RunAsync("docker", ["info"]);
        }
        catch (Win32Exception ex)
        {
            _dockerDetail = "Docker executable was not found, so isolated Mosquitto ACL was not started. " + ex.Message;
            return;
        }

        var dockerContinue = docker;
        if (dockerContinue.ExitCode != 0)
        {
            _dockerDetail = "Docker is not available. Isolated Mosquitto ACL was not started. " + Trim(dockerContinue.StdErr);
            return;
        }

        _tempDir = Path.Combine(Path.GetTempPath(), "ss-g1-mqtt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        var fixture = FixtureDirectory();
        File.Copy(Path.Combine(fixture, "mosquitto.conf"), Path.Combine(_tempDir, "mosquitto.conf"));
        File.Copy(Path.Combine(fixture, "acl"), Path.Combine(_tempDir, "acl"));

        _password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _containerName = "ss-g1-mqtt-" + Guid.NewGuid().ToString("N")[..12];
        var script =
            "cp /mosquitto/config/acl /tmp/acl && " +
            "mosquitto_passwd -b -c /tmp/passwd srv \"$MQTT_PASSWORD\" && " +
            "mosquitto_passwd -b /tmp/passwd deva \"$MQTT_PASSWORD\" && " +
            "mosquitto_passwd -b /tmp/passwd devb \"$MQTT_PASSWORD\" && " +
            "chown mosquitto:mosquitto /tmp/passwd /tmp/acl && " +
            "chmod 0640 /tmp/passwd && " +
            "exec mosquitto -c /mosquitto/config/mosquitto.conf";

        var started = await RunAsync(
            "docker",
            [
                "run", "-d", "--name", _containerName,
                "--entrypoint", "sh",
                "-p", "127.0.0.1::1883",
                "-e", "MQTT_PASSWORD=" + _password,
                "-v", _tempDir + ":/mosquitto/config:ro",
                "eclipse-mosquitto:2",
                "-c", script
            ]);
        if (started.ExitCode != 0)
        {
            _dockerDetail = "docker run failed. " + Trim(started.StdErr + started.StdOut);
            return;
        }

        var mapped = await WaitForPublishedPortAsync(_containerName);
        if (!mapped.Published)
        {
            _dockerDetail = "Broker port was not published. " + mapped.Detail + " logs=" + await ContainerLogsAsync(_containerName);
            return;
        }

        _port = mapped.Port;

        var ready = await WaitForTcpAsync(_port, TimeSpan.FromSeconds(30));
        _dockerReady = ready;
        _dockerDetail = ready
            ? $"Isolated Mosquitto accepted TCP on 127.0.0.1:{_port}. Password was generated at runtime and is not recorded."
            : $"Isolated Mosquitto did not accept TCP on 127.0.0.1:{_port}. logs={await ContainerLogsAsync(_containerName)}";
    }

    public async Task DisposeAsync()
    {
        if (_containerName is not null)
        {
            try
            {
                await RunAsync("docker", ["rm", "-f", _containerName]);
            }
            catch (Win32Exception)
            {
                // Docker disappeared between setup and cleanup.
            }
        }
        if (_tempDir is not null && Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Docker_Probe_Does_Not_Claim_Acl_Success_By_Itself()
    {
        Assert.False(string.IsNullOrWhiteSpace(_dockerDetail));
        Assert.DoesNotContain(_password ?? "no-password-in-detail", _dockerDetail, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Anonymous_Connect_Is_Rejected()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        using var client = new MqttClientFactory().CreateMqttClient();
        var result = await client.ConnectAsync(Options("anon-" + Guid.NewGuid().ToString("N")[..8], user: null));
        Assert.NotEqual(MqttClientConnectResultCode.Success, result.ResultCode);
    }

    [SkippableFact]
    public async Task DeviceA_Cannot_Publish_On_DeviceB_State()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        using var client = await ConnectAsync("deva", "deva-pub");
        var publish = await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{DeviceB}/state")
            .WithPayload("{\"messageType\":\"STATE\"}")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
        Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, publish.ReasonCode);
    }

    [SkippableFact]
    public async Task DeviceA_Does_Not_Receive_DeviceB_Commands()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        var foreign = "{\"messageType\":\"EXECUTE_COMMAND\",\"commandId\":\"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb\"}";
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = await ConnectAsync("deva", "deva-sub");
        device.ApplicationMessageReceivedAsync += e =>
        {
            received.TrySetResult(e.ApplicationMessage.ConvertPayloadToString());
            return Task.CompletedTask;
        };
        await device.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic($"devices/{DeviceB}/commands")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        using var server = await ConnectAsync("srv", "srv-foreign");
        var published = await server.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{DeviceB}/commands")
            .WithPayload(foreign)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build());
        Assert.Equal(MqttClientPublishReasonCode.Success, published.ReasonCode);

        var late = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.NotSame(received.Task, late);
    }

    [SkippableFact]
    public async Task Server_Command_Reaches_DeviceA_At_Qos1_Without_Retain()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        var payload = "{\"messageType\":\"EXECUTE_COMMAND\",\"commandId\":\"22222222-2222-4222-8222-222222222222\"}";
        var received = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = await ConnectAsync("deva", "deva-cmd");
        device.ApplicationMessageReceivedAsync += e =>
        {
            received.TrySetResult(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        var sub = await device.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic($"devices/{DeviceA}/commands")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
        Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS1, Assert.Single(sub.Items).ResultCode);

        using var server = await ConnectAsync("srv", "srv-cmd");
        var published = await server.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{DeviceA}/commands")
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build());
        Assert.Equal(MqttClientPublishReasonCode.Success, published.ReasonCode);

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(8)));
        Assert.Same(received.Task, completed);
        var message = await received.Task;
        Assert.False(message.Retain);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);
        Assert.Equal(payload, message.ConvertPayloadToString());
    }

    [SkippableFact]
    public async Task Unretained_Command_Is_Not_Delivered_To_A_Later_Subscriber()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        using (var server = await ConnectAsync("srv", "srv-noretain"))
        {
            await server.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic($"devices/{DeviceA}/commands")
                .WithPayload("{\"messageType\":\"EXECUTE_COMMAND\"}")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build());
        }

        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = await ConnectAsync("deva", "deva-late");
        device.ApplicationMessageReceivedAsync += _ =>
        {
            received.TrySetResult();
            return Task.CompletedTask;
        };
        await device.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic($"devices/{DeviceA}/commands")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        var late = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.NotSame(received.Task, late);
    }

    [SkippableFact]
    public async Task Heartbeat_Publish_Uses_Qos0()
    {
        Skip.IfNot(_dockerReady, _dockerDetail);
        var received = new TaskCompletionSource<MqttApplicationMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = await ConnectAsync("srv", "srv-hb");
        server.ApplicationMessageReceivedAsync += e =>
        {
            received.TrySetResult(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        await server.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic($"devices/{DeviceA}/state")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build());

        using var device = await ConnectAsync("deva", "deva-hb");
        await device.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{DeviceA}/state")
            .WithPayload("{\"messageType\":\"HEARTBEAT\"}")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .WithRetainFlag(false)
            .Build());

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(8)));
        Assert.Same(received.Task, completed);
        Assert.Equal(MqttQualityOfServiceLevel.AtMostOnce, (await received.Task).QualityOfServiceLevel);
    }

    private async Task<IMqttClient> ConnectAsync(string user, string clientPrefix)
    {
        var client = new MqttClientFactory().CreateMqttClient();
        var result = await client.ConnectAsync(Options(clientPrefix + "-" + Guid.NewGuid().ToString("N")[..8], user));
        Assert.Equal(MqttClientConnectResultCode.Success, result.ResultCode);
        return client;
    }

    private MqttClientOptions Options(string clientId, string? user)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", _port)
            .WithClientId(clientId)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithCleanStart();
        if (user is not null)
            builder.WithCredentials(user, _password);
        return builder.Build();
    }

    private static string FixtureDirectory()
    {
        var fromOutput = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "mosquitto"));
        if (Directory.Exists(fromOutput))
            return fromOutput;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "security_backEnd",
                "API.IntegrationTests",
                "mosquitto");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Mosquitto fixture directory was not found.");
    }

    private static async Task<string> ContainerLogsAsync(string containerName)
    {
        var logs = await RunAsync("docker", ["logs", containerName]);
        return Trim(logs.StdErr + logs.StdOut);
    }

    private static async Task<(bool Published, int Port, string Detail)> WaitForPublishedPortAsync(string containerName)
    {
        var last = "";
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var mapped = await RunAsync("docker", ["port", containerName, "1883/tcp"]);
            last = Trim(mapped.StdErr + mapped.StdOut);
            if (mapped.ExitCode == 0 && TryParsePort(mapped.StdOut, out var port))
                return (true, port, last);
            await Task.Delay(500);
        }

        return (false, 0, last);
    }

    private static bool TryParsePort(string text, out int port)
    {
        port = 0;
        var line = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (line is null)
            return false;
        var colon = line.LastIndexOf(':');
        return colon >= 0 && int.TryParse(line[(colon + 1)..].Trim(), out port);
    }

    private static async Task<bool> WaitForTcpAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("127.0.0.1", port);
                return true;
            }
            catch (SocketException)
            {
                await Task.Delay(500);
            }
        }

        return false;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string file,
        IReadOnlyList<string> args)
    {
        var start = new ProcessStartInfo
        {
            FileName = file,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Failed to start " + file);
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    private static string Trim(string text)
    {
        var compact = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return compact.Length <= 400 ? compact : compact[..400];
    }
}
