using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class DeviceOwnershipHttpTests : IAsyncLifetime
{
    private readonly AuthApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Admin_Links_Device_As_Pending_Without_Arming_And_Queues_OwnershipSync()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-001", "جهاز ربط");

        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });

        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(link);
        Assert.Equal(DbStatusCodes.Ownership.Pending, data.GetProperty("ownershipStatus").GetString());
        Assert.Equal(1, data.GetProperty("ownershipVersion").GetInt64());
        Assert.Equal(0, data.GetProperty("confirmedOwnershipVersion").GetInt64());
        Assert.Equal(DbStatusCodes.Arm.Unknown, data.GetProperty("armState").GetString());
        Assert.False(data.GetProperty("canControl").GetBoolean());
        Assert.True(data.GetProperty("ownershipSyncQueued").GetBoolean());
        Assert.Equal(OwnershipSyncDraft.MqttMessageType, data.GetProperty("ownershipSyncMessageType").GetString());
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Registered, data.GetProperty("deviceLifecycleStatus").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Pending, ownership.Status);
        Assert.Null(ownership.ActivatedAt);
        Assert.Null(ownership.EndedAt);
        Assert.Equal(AuthApiFactory.OwnerId, ownership.OwnerUserId);

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        Assert.Equal(1, device.CurrentOwnershipVersion);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Registered, device.LifecycleStatus);

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Arm.Unknown, state.ArmState);
        Assert.Equal(0, state.ConfirmedOwnershipVersion);

        var outbox = await db.OutboxMessages.SingleAsync(m =>
            m.MessageType == OwnershipSyncDraft.OutboxPublishMessageType
            && m.AggregateId == ownership.Id);
        Assert.Null(outbox.ProcessedAt);
        Assert.Equal(OwnershipSyncDraft.OutboxAggregateType, outbox.AggregateType);
        using (var payloadDoc = JsonDocument.Parse(outbox.Payload))
        {
            var root = payloadDoc.RootElement;
            Assert.Equal(OwnershipSyncDraft.SchemaVersion, root.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(OwnershipSyncDraft.MqttMessageType, root.GetProperty("messageType").GetString());
            Assert.Equal(deviceId, root.GetProperty("deviceId").GetGuid());
            Assert.Equal(1, root.GetProperty("ownershipVersion").GetInt64());
            Assert.True(root.GetProperty("isLinked").GetBoolean());
            Assert.False(root.TryGetProperty("ownerUserId", out _));
        }
        Assert.DoesNotContain(AuthApiFactory.OwnerId.ToString(), outbox.Payload, StringComparison.OrdinalIgnoreCase);

        var audit = await db.AuditLogs.SingleAsync(a =>
            a.Action == AuditActions.DeviceLinked && a.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.AuditResult.Success, audit.Result);
        Assert.Equal(AuthApiFactory.AdminId, audit.ActorUserId);
        Assert.DoesNotContain("password", audit.Details ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Owner_Sees_Limited_Pending_List_Foreign_Owner_Sees_Nothing()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-list", "جهاز عرض معلق");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var mine = await ownerClient.GetAsync("/api/owner/devices");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var items = await AuthApiFactory.ReadDataAsync(mine);
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(1, items.GetArrayLength());
        var row = items[0];
        Assert.Equal(deviceId, row.GetProperty("deviceId").GetGuid());
        Assert.Equal("جهاز عرض معلق", row.GetProperty("displayName").GetString());
        Assert.Equal(DbStatusCodes.Ownership.Pending, row.GetProperty("ownershipStatus").GetString());
        Assert.True(row.GetProperty("awaitingOwnershipAck").GetBoolean());
        Assert.False(row.GetProperty("canControl").GetBoolean());
        Assert.Equal("الربط قيد الاكتمال", row.GetProperty("linkStatusMessage").GetString());
        Assert.False(row.TryGetProperty("armState", out _));
        Assert.False(row.TryGetProperty("sirenState", out _));
        Assert.False(row.TryGetProperty("events", out _));
        Assert.False(row.TryGetProperty("commands", out _));

        // Foreign owner isolation
        var foreignOwnerId = await CreateExtraOwnerAsync("auth_owner_foreign");
        using var foreignClient = await CreateAuthorizedClientAsync("auth_owner_foreign");
        var foreignList = await foreignClient.GetAsync("/api/owner/devices");
        Assert.Equal(HttpStatusCode.OK, foreignList.StatusCode);
        var foreignItems = await AuthApiFactory.ReadDataAsync(foreignList);
        Assert.Equal(0, foreignItems.GetArrayLength());
        Assert.NotEqual(AuthApiFactory.OwnerId, foreignOwnerId);
    }

    [Fact]
    public async Task Confirming_Ownership_Version_Activates_Without_Changing_Arm()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-ack", "جهاز إقرار");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var ownershipVersion = (await AuthApiFactory.ReadDataAsync(link)).GetProperty("ownershipVersion").GetInt64();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
            var result = await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion);
            Assert.True(result.Activated);
            Assert.Equal(DbStatusCodes.Ownership.Active, result.OwnershipStatus);
            Assert.True(result.CanControl);
            Assert.Equal(DbStatusCodes.Arm.Unknown, result.ArmState);
            Assert.Equal(ownershipVersion, result.ConfirmedOwnershipVersion);
        }

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Active, ownership.Status);
        Assert.NotNull(ownership.ActivatedAt);
        Assert.Null(ownership.EndedAt);

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Active, device.LifecycleStatus);

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Arm.Unknown, state.ArmState);
        Assert.Equal(ownershipVersion, state.ConfirmedOwnershipVersion);

        var audit = await db.AuditLogs.SingleAsync(a =>
            a.Action == AuditActions.OwnershipActivated && a.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.AuditResult.Success, audit.Result);

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var mine = await ownerClient.GetAsync("/api/owner/devices");
        var items = await AuthApiFactory.ReadDataAsync(mine);
        var row = items[0];
        Assert.Equal(DbStatusCodes.Ownership.Active, row.GetProperty("ownershipStatus").GetString());
        Assert.False(row.GetProperty("awaitingOwnershipAck").GetBoolean());
        Assert.True(row.GetProperty("canControl").GetBoolean());
        Assert.False(row.TryGetProperty("armState", out _));
        Assert.True(row.TryGetProperty("indicators", out var indicators));
        Assert.Equal(JsonValueKind.Object, indicators.ValueKind);
        Assert.Equal(DbStatusCodes.Arm.Unknown, indicators.GetProperty("armState").GetString());
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, indicators.GetProperty("healthState").GetString());
        Assert.Equal(MqttInboundDraft.ConnectionStatus.Offline, indicators.GetProperty("connectionStatus").GetString());
    }

    [Fact]
    public async Task Confirm_With_Wrong_Version_Does_Not_Activate()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-wrong-ack", "جهاز إقرار خاطئ");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
        var result = await ownerships.ConfirmOwnershipVersionAsync(deviceId, confirmedOwnershipVersion: 0);
        Assert.False(result.Activated);
        Assert.Equal(DbStatusCodes.Ownership.Pending, result.OwnershipStatus);
        Assert.False(result.CanControl);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Ownership.Pending, ownership.Status);
        Assert.Null(ownership.ActivatedAt);
    }

    [Fact]
    public async Task Second_Open_Link_Is_Conflict()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-dup", "جهاز مكرر الربط");

        var first = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var foreignOwnerId = await CreateExtraOwnerAsync("auth_owner_second");
        var second = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = foreignOwnerId
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var json = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DEVICE_ALREADY_LINKED", json.GetProperty("error").GetProperty("code").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.DeviceOwnerships.CountAsync(o => o.DeviceId == deviceId));
    }

    [Fact]
    public async Task Owner_Is_Forbidden_On_Admin_Link_And_Admin_Is_Forbidden_On_Owner_List()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-link-forbid", "جهاز صلاحيات");

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var ownerLink = await ownerClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Forbidden, ownerLink.StatusCode);

        var adminList = await adminClient.GetAsync("/api/owner/devices");
        Assert.Equal(HttpStatusCode.Forbidden, adminList.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Link_And_List_Are_401()
    {
        var link = await _client.PostAsJsonAsync($"/api/admin/devices/{Guid.NewGuid()}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Unauthorized, link.StatusCode);

        var list = await _client.GetAsync("/api/owner/devices");
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
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

    private async Task<Guid> CreateExtraOwnerAsync(string userName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null)
            return existing.Id;

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            FullName = userName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var create = await users.CreateAsync(user, AuthApiFactory.Password);
        Assert.True(create.Succeeded);
        await users.AddToRoleAsync(user, DbStatusCodes.RoleCodes.Owner);
        return user.Id;
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
