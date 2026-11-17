using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Application.Services.MqttServices;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace API.IntegrationTests;

/// <summary>
/// Real PostgreSQL process stop/start on an isolated Docker instance.
/// Does not stop the Windows service that hosts the shared local databases.
/// </summary>
public sealed class IsolatedPostgresKillRecoveryTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private string? _container;
    private string? _connectionString;
    private AuthApiFactory? _factory;
    private HttpClient? _client;
    private bool _ready;
    private string _detail = "Docker Postgres was not started.";

    public async Task InitializeAsync()
    {
        try
        {
            var info = await RunAsync("docker", ["info"]);
            if (info.ExitCode != 0)
            {
                _detail = "Docker is not available. Isolated Postgres kill was not started.";
                return;
            }
        }
        catch (Win32Exception ex)
        {
            _detail = "Docker executable was not found. " + ex.Message;
            return;
        }

        var port = ReserveLoopbackPort();
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
        _container = "ss-pg-kill-" + Guid.NewGuid().ToString("N")[..12];
        var started = await RunAsync("docker",
        [
            "run", "-d", "--name", _container,
            "-e", "POSTGRES_PASSWORD=" + password,
            "-e", "POSTGRES_DB=esp32_security_pg_kill_test",
            "-p", $"127.0.0.1:{port}:5432",
            "postgres:16-alpine"
        ]);
        if (started.ExitCode != 0)
        {
            _detail = "docker run postgres failed. " + Trim(started.StdErr + started.StdOut);
            return;
        }

        _connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = port,
            Database = "esp32_security_pg_kill_test",
            Username = "postgres",
            Password = password
        }.ConnectionString;

        if (!await WaitForPostgresAsync(_connectionString, TimeSpan.FromSeconds(60)))
        {
            _detail = "Isolated Postgres did not accept connections. " + Trim(await LogsAsync(_container));
            return;
        }

        // Program seeds roles on host start when CanConnectAsync is true. A brand-new
        // Docker database must already have migrations before WebApplicationFactory starts.
        var migrateOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        await using (var migrateDb = new AppDbContext(migrateOptions))
            await migrateDb.Database.MigrateAsync();

        _factory = new AuthApiFactory(
            services => services.AddSingleton<IMqttOutboundPublisher>(_publisher),
            connectionString: _connectionString);
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
        _ready = true;
        _detail = $"Isolated Postgres accepted TCP on 127.0.0.1:{port} and migrated esp32_security_pg_kill_test.";
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (!string.IsNullOrEmpty(_container))
        {
            try
            {
                await RunAsync("docker", ["rm", "-f", _container]);
            }
            catch (Win32Exception)
            {
            }
        }
    }

    [SkippableFact]
    public async Task Outbox_QueuedPhases_Survive_Real_Postgres_Stop_And_Restart()
    {
        Skip.IfNot(_ready, _detail);
        Assert.NotNull(_factory);
        Assert.NotNull(_client);
        Assert.NotNull(_connectionString);
        Assert.NotNull(_container);

        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-pg-kill-01", "قتل قاعدة");
        var eventUid = Guid.NewGuid();
        var start = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);
        var ended = await IngestAlarmAsync(
            deviceId, eventUid, 1, false, false, null, AlarmEventDraft.AlarmPhase.Ended);
        Assert.False(ended.NotificationCreated);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == start.OutboxMessageId);
            var queued = EventPersistedPayload.ReadQueue(outbox.Payload);
            Assert.Contains(AlarmEventDraft.AlarmPhase.Active, queued);
            Assert.Contains(AlarmEventDraft.AlarmPhase.Ended, queued);
        }

        var stopped = await RunAsync("docker", ["kill", _container]);
        Assert.Equal(0, stopped.ExitCode);
        NpgsqlConnection.ClearAllPools();
        await Task.Delay(1000);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await using var dead = new NpgsqlConnection(_connectionString);
            await dead.OpenAsync();
            await using var cmd = dead.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
        });

        var started = await RunAsync("docker", ["start", _container]);
        Assert.Equal(0, started.ExitCode);
        Assert.True(await WaitForPostgresAsync(_connectionString, TimeSpan.FromSeconds(60)));
        NpgsqlConnection.ClearAllPools();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alarm = await db.AlarmEvents.SingleAsync(a => a.EventUid == eventUid);
            Assert.Equal(DbStatusCodes.AlarmEvent.Ended, alarm.Status);
            Assert.Equal(1, await db.Notifications.CountAsync(n => n.AlarmEventId == alarm.Id));
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == start.OutboxMessageId);
            Assert.Null(outbox.ProcessedAt);
            var queued = EventPersistedPayload.ReadQueue(outbox.Payload);
            Assert.Contains(AlarmEventDraft.AlarmPhase.Active, queued);
            Assert.Contains(AlarmEventDraft.AlarmPhase.Ended, queued);

            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        Assert.Equal(2, _publisher.Messages.Count);
        var phases = _publisher.Messages
            .Select(m => JsonDocument.Parse(m.PayloadJson).RootElement.GetProperty("alarmPhase").GetString())
            .ToArray();
        Assert.Equal(AlarmEventDraft.AlarmPhase.Active, phases[0]);
        Assert.Equal(AlarmEventDraft.AlarmPhase.Ended, phases[1]);
    }

    private async Task<AlarmEventIngestResultDto> IngestAlarmAsync(
        Guid deviceId,
        Guid eventUid,
        long ownershipVersion,
        bool wasBuffered,
        bool clockTrusted,
        DateTimeOffset? occurredDeviceAt,
        string? alarmPhase = null)
    {
        Assert.NotNull(_factory);
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["deviceId"] = deviceId,
            ["messageId"] = Guid.NewGuid(),
            ["messageType"] = AlarmEventDraft.MessageTypes.AlarmEvent,
            ["bootId"] = "boot-pg-kill",
            ["eventUid"] = eventUid,
            ["ownershipVersion"] = ownershipVersion,
            ["sensorKey"] = "vibration-1",
            ["wasBuffered"] = wasBuffered,
            ["clockTrusted"] = clockTrusted,
            ["occurredDeviceAt"] = occurredDeviceAt
        };
        if (alarmPhase is not null)
            payload["alarmPhase"] = alarmPhase;

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAlarmEventIngestService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = AlarmEventDraft.Topics.Events(deviceId),
            PayloadJson = JsonSerializer.Serialize(payload)
        });
    }

    private async Task<Guid> RegisterActivateAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        Assert.NotNull(_factory);
        var reg = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid,
            displayName,
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = "AlarmSecret!99"
            },
            settings = new
            {
                eventBufferCapacity = 64,
                commandTtlSeconds = 120,
                alarmRule = new { schemaVersion = 1 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var deviceId = (await AuthApiFactory.ReadDataAsync(reg)).GetProperty("id").GetGuid();

        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var state = await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = MqttInboundDraft.Topics.State(deviceId),
            PayloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = MqttInboundDraft.MessageTypes.State,
                bootId = "boot-activate",
                stateSequence = 1,
                confirmedOwnershipVersion = 1,
                armState = DbStatusCodes.Arm.Disarmed,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = DbStatusCodes.Diagnostic.Unknown,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, state.Outcome);
        Assert.True(state.OwnershipActivated);
        return deviceId;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string userName)
    {
        Assert.NotNull(_client);
        Assert.NotNull(_factory);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var access = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("accessToken").GetString()!;
        var authorized = _factory.CreateJsonClient();
        authorized.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return authorized;
    }

    private static async Task<bool> WaitForPostgresAsync(string connectionString, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        return false;
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        string fileName,
        IReadOnlyList<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start " + fileName);
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    private static async Task<string> LogsAsync(string container)
    {
        var logs = await RunAsync("docker", ["logs", "--tail", "40", container]);
        return logs.StdOut + logs.StdErr;
    }

    private static string Trim(string text)
        => text.Length <= 400 ? text : text[..400];
}
