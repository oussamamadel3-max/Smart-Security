using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class OwnerDeviceStateHttpTests : IAsyncLifetime
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
    public async Task Active_Owner_Sees_Four_Independent_Indicators_And_Detail()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(adminClient, "hw-uid-owner-state-01", "جهاز مؤشرات");

        await IngestStateAsync(
            deviceId,
            stateSequence: 4,
            arm: DbStatusCodes.Arm.Armed,
            health: DbStatusCodes.Diagnostic.Unknown,
            confirmedOwnershipVersion: 1);

        // Force offline classification without changing arm/health.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow.AddHours(-2);
            await db.SaveChangesAsync();
        }

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var listResponse = await ownerClient.GetAsync("/api/owner/devices");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await AuthApiFactory.ReadDataAsync(listResponse);
        var row = list[0];
        Assert.Equal(DbStatusCodes.Ownership.Active, row.GetProperty("ownershipStatus").GetString());
        Assert.True(row.TryGetProperty("indicators", out var indicators));
        Assert.Equal(JsonValueKind.Object, indicators.ValueKind);
        Assert.Equal(MqttInboundDraft.ConnectionStatus.Offline, indicators.GetProperty("connectionStatus").GetString());
        Assert.Equal(DbStatusCodes.Arm.Armed, indicators.GetProperty("armState").GetString());
        Assert.Equal(DbStatusCodes.Siren.Off, indicators.GetProperty("sirenState").GetString());
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, indicators.GetProperty("healthState").GetString());
        Assert.NotEqual(DbStatusCodes.Diagnostic.Healthy, indicators.GetProperty("healthState").GetString());
        Assert.Equal(3600, indicators.GetProperty("heartbeatTimeoutSeconds").GetInt32());

        var detailResponse = await ownerClient.GetAsync($"/api/owner/devices/{deviceId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await AuthApiFactory.ReadDataAsync(detailResponse);
        Assert.Equal(deviceId, detail.GetProperty("deviceId").GetGuid());
        var detailIndicators = detail.GetProperty("indicators");
        Assert.Equal(MqttInboundDraft.ConnectionStatus.Offline, detailIndicators.GetProperty("connectionStatus").GetString());
        Assert.Equal(DbStatusCodes.Arm.Armed, detailIndicators.GetProperty("armState").GetString());
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, detailIndicators.GetProperty("healthState").GetString());
    }

    [Fact]
    public async Task Pending_Owner_Detail_Remains_Limited_Without_Indicators()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-uid-owner-pending-detail", "معلق");
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);

        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var detailResponse = await ownerClient.GetAsync($"/api/owner/devices/{deviceId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await AuthApiFactory.ReadDataAsync(detailResponse);
        Assert.Equal(DbStatusCodes.Ownership.Pending, detail.GetProperty("ownershipStatus").GetString());
        Assert.True(detail.TryGetProperty("indicators", out var indicators));
        Assert.Equal(JsonValueKind.Null, indicators.ValueKind);
        Assert.False(detail.GetProperty("canControl").GetBoolean());
    }

    [Fact]
    public async Task Foreign_Owner_Cannot_Read_Device_Detail()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(adminClient, "hw-uid-owner-foreign-detail", "عزل");

        await CreateExtraOwnerAsync("auth_owner_state_foreign");
        using var foreignClient = await CreateAuthorizedClientAsync("auth_owner_state_foreign");
        var detail = await foreignClient.GetAsync($"/api/owner/devices/{deviceId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Healthy_Is_Shown_Only_When_Device_Reports_Healthy()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterAndActivateAsync(adminClient, "hw-uid-owner-health", "تشخيص");

        using var ownerBefore = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var before = await AuthApiFactory.ReadDataAsync(await ownerBefore.GetAsync($"/api/owner/devices/{deviceId}"));
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, before.GetProperty("indicators").GetProperty("healthState").GetString());

        await IngestStateAsync(
            deviceId,
            stateSequence: 2,
            arm: DbStatusCodes.Arm.Disarmed,
            health: DbStatusCodes.Diagnostic.Healthy,
            confirmedOwnershipVersion: 1);

        using var ownerAfter = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var after = await AuthApiFactory.ReadDataAsync(await ownerAfter.GetAsync($"/api/owner/devices/{deviceId}"));
        Assert.Equal(DbStatusCodes.Diagnostic.Healthy, after.GetProperty("indicators").GetProperty("healthState").GetString());
        Assert.Equal(DbStatusCodes.Arm.Disarmed, after.GetProperty("indicators").GetProperty("armState").GetString());
    }

    private async Task<Guid> RegisterAndActivateAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var deviceId = await RegisterDeviceAsync(adminClient, hardwareUid, displayName);
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var ownershipVersion = (await AuthApiFactory.ReadDataAsync(link)).GetProperty("ownershipVersion").GetInt64();

        await using var scope = _factory.Services.CreateAsyncScope();
        var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
        var confirm = await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion);
        Assert.True(confirm.Activated);
        return deviceId;
    }

    private async Task IngestStateAsync(
        Guid deviceId,
        long stateSequence,
        string arm,
        string health,
        long confirmedOwnershipVersion)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IDeviceInboundTelemetryService>();
        var result = await ingest.IngestAsync(new DeviceInboundEnvelope
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
                bootId = "boot-owner",
                stateSequence,
                confirmedOwnershipVersion,
                armState = arm,
                sirenState = DbStatusCodes.Siren.Off,
                healthState = health,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, result.Outcome);
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

    private static async Task<Guid> RegisterDeviceAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var response = await adminClient.PostAsJsonAsync("/api/admin/devices", new
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
                eventBufferCapacity = 64,
                commandTtlSeconds = 120,
                alarmRule = new { schemaVersion = 1 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await AuthApiFactory.ReadDataAsync(response)).GetProperty("id").GetGuid();
    }
}
