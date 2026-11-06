using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Interfaces.Services;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class DeviceArchiveHttpTests : IAsyncLifetime
{
    private readonly RecordingDeviceBrokerAccessService _broker = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public DeviceArchiveHttpTests()
    {
        _factory = new AuthApiFactory(services =>
        {
            var existing = services.Where(d => d.ServiceType == typeof(IDeviceBrokerAccessService)).ToList();
            foreach (var descriptor in existing)
                services.Remove(descriptor);
            services.AddSingleton<IDeviceBrokerAccessService>(_broker);
        });
    }

    public async Task InitializeAsync()
    {
        _broker.FailNext = false;
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Admin_Archives_Unlinked_Device_Revokes_Credential_Broker_And_Keeps_History()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-archive-001", "جهاز أرشفة");

        var archive = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        Assert.Contains("no-store", archive.Headers.CacheControl?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var data = await AuthApiFactory.ReadDataAsync(archive);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Retired, data.GetProperty("lifecycleStatus").GetString());
        Assert.False(data.GetProperty("ownershipEnded").GetBoolean());
        Assert.False(data.GetProperty("ownershipSyncQueued").GetBoolean());
        Assert.Equal(1, data.GetProperty("credentialsRevoked").GetInt32());
        Assert.False(data.GetProperty("hasActiveCredential").GetBoolean());
        Assert.True(data.GetProperty("brokerAccessRevoked").GetBoolean());
        Assert.Equal(0, data.GetProperty("currentOwnershipVersion").GetInt64());
        Assert.True(data.TryGetProperty("retiredAt", out _));

        Assert.Single(_broker.Calls);
        Assert.Equal(deviceId, _broker.Calls[0].DeviceId);
        Assert.Equal("hw-uid-archive-001", _broker.Calls[0].HardwareUid);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Retired, device.LifecycleStatus);
        Assert.NotNull(device.RetiredAt);

        // History retained — no physical delete.
        Assert.True(await db.DeviceStates.AnyAsync(s => s.DeviceId == deviceId));
        Assert.True(await db.DeviceSettings.AnyAsync(s => s.DeviceId == deviceId));
        var credential = await db.DeviceCredentials.SingleAsync(c => c.DeviceId == deviceId);
        Assert.NotNull(credential.RevokedAt);
        Assert.False(string.IsNullOrWhiteSpace(credential.Verifier));

        var audit = await db.AuditLogs.SingleAsync(a =>
            a.Action == AuditActions.DeviceArchived && a.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.AuditResult.Success, audit.Result);
        Assert.Equal(AuthApiFactory.AdminId, audit.ActorUserId);
        Assert.Contains("RETIRED", audit.Details ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("password", audit.Details ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("verifier", audit.Details ?? "", StringComparison.OrdinalIgnoreCase);

        // Re-link rejected after archive.
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Conflict, link.StatusCode);
        var linkJson = await link.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DEVICE_RETIRED", linkJson.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_Archives_Pending_Ownership_Cancels_Link_And_Queues_Unlink_Sync()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-archive-pending", "أرشفة معلق");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        using var ownerBefore = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var listBefore = await AuthApiFactory.ReadDataAsync(await ownerBefore.GetAsync("/api/owner/devices"));
        Assert.Equal(1, listBefore.GetArrayLength());

        var archive = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(archive);
        Assert.True(data.GetProperty("ownershipEnded").GetBoolean());
        Assert.Equal(DbStatusCodes.Ownership.Cancelled, data.GetProperty("closedOwnershipStatus").GetString());
        Assert.True(data.GetProperty("ownershipSyncQueued").GetBoolean());
        Assert.Equal(2, data.GetProperty("currentOwnershipVersion").GetInt64());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Cancelled, ownership.Status);
        Assert.NotNull(ownership.EndedAt);
        Assert.Equal(AuthApiFactory.AdminId, ownership.EndedByUserId);

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        Assert.Equal(2, device.CurrentOwnershipVersion);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Retired, device.LifecycleStatus);

        var unlinkOutbox = await db.OutboxMessages
            .Where(m => m.MessageType == OwnershipSyncDraft.OutboxPublishMessageType)
            .OrderByDescending(m => m.CreatedAt)
            .FirstAsync();
        using (var payloadDoc = JsonDocument.Parse(unlinkOutbox.Payload))
        {
            var root = payloadDoc.RootElement;
            Assert.Equal(OwnershipSyncDraft.MqttMessageType, root.GetProperty("messageType").GetString());
            Assert.Equal(deviceId, root.GetProperty("deviceId").GetGuid());
            Assert.Equal(2, root.GetProperty("ownershipVersion").GetInt64());
            Assert.False(root.GetProperty("isLinked").GetBoolean());
        }

        // Owner no longer sees the device.
        using var ownerAfter = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var listAfter = await AuthApiFactory.ReadDataAsync(await ownerAfter.GetAsync("/api/owner/devices"));
        Assert.Equal(0, listAfter.GetArrayLength());
    }

    [Fact]
    public async Task Admin_Archives_Active_Ownership_Ends_Access()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-archive-active", "أرشفة فعال");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
            var result = await ownerships.ConfirmOwnershipVersionAsync(deviceId, 1);
            Assert.True(result.Activated);
        }

        var archive = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(archive);
        Assert.Equal(DbStatusCodes.Ownership.Ended, data.GetProperty("closedOwnershipStatus").GetString());
        Assert.Equal(2, data.GetProperty("currentOwnershipVersion").GetInt64());

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Ended, ownership.Status);
        Assert.NotNull(ownership.ActivatedAt);
        Assert.NotNull(ownership.EndedAt);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Retired,
            (await db.Devices.SingleAsync(d => d.Id == deviceId)).LifecycleStatus);
    }

    [Fact]
    public async Task Archive_Twice_Is_Conflict_And_Broker_Failure_Rolls_Back()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-archive-twice", "أرشفة مكررة");

        var first = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DEVICE_ALREADY_RETIRED", secondJson.GetProperty("error").GetProperty("code").GetString());

        var failDeviceId = await RegisterDeviceAsync(adminClient, "hw-uid-archive-broker-fail", "فشل وسيط");
        _broker.FailNext = true;
        try
        {
            var failed = await adminClient.PostAsync($"/api/admin/devices/{failDeviceId}/archive", null);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            var failJson = await failed.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("DEVICE_BROKER_REVOKE_FAILED", failJson.GetProperty("error").GetProperty("code").GetString());
        }
        finally
        {
            _broker.FailNext = false;
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var device = await db.Devices.SingleAsync(d => d.Id == failDeviceId);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Registered, device.LifecycleStatus);
        Assert.Null(device.RetiredAt);
        var credential = await db.DeviceCredentials.SingleAsync(c => c.DeviceId == failDeviceId);
        Assert.Null(credential.RevokedAt);
        Assert.False(await db.AuditLogs.AnyAsync(a =>
            a.Action == AuditActions.DeviceArchived && a.DeviceId == failDeviceId));
    }

    [Fact]
    public async Task Owner_Is_Forbidden_And_Unknown_Device_Is_404()
    {
        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var ownerArchive = await ownerClient.PostAsync($"/api/admin/devices/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.Forbidden, ownerArchive.StatusCode);

        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var missing = await adminClient.PostAsync($"/api/admin/devices/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var json = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DEVICE_NOT_FOUND", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unauthenticated_Archive_Is_401()
    {
        var res = await _client.PostAsync($"/api/admin/devices/{Guid.NewGuid()}/archive", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private async Task<Guid> RegisterDeviceAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var res = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid,
            displayName,
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = "MqttSecret!99"
            },
            settings = new
            {
                alarmRule = new { schemaVersion = 1 },
                eventBufferCapacity = 8,
                commandTtlSeconds = 20
            }
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await AuthApiFactory.ReadDataAsync(res)).GetProperty("id").GetGuid();
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(string userName)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var access = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("accessToken").GetString()!;

        var client = _factory.CreateJsonClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        return client;
    }
}
