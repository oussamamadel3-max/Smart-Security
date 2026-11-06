using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Domain.Constants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class DeviceRegistrationHttpTests : IAsyncLifetime
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
    public async Task Admin_Registers_Device_With_Credential_Settings_State_And_Audit()
    {
        const string mqttPassword = "MqttSecret!99";
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);

        var res = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid = "hw-uid-register-001",
            displayName = "جهاز تسجيل اختبار",
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = mqttPassword
            },
            settings = new
            {
                alarmRule = new { schemaVersion = 1, note = "approved-for-lab-device" },
                eventBufferCapacity = 16,
                commandTtlSeconds = 45
            }
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Contains("no-store", res.Headers.CacheControl?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var data = await AuthApiFactory.ReadDataAsync(res);
        var deviceId = data.GetProperty("id").GetGuid();
        Assert.Equal("hw-uid-register-001", data.GetProperty("hardwareUid").GetString());
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Registered, data.GetProperty("lifecycleStatus").GetString());
        Assert.Equal(0, data.GetProperty("currentOwnershipVersion").GetInt64());
        Assert.Equal(1, data.GetProperty("settingsVersion").GetInt64());
        Assert.Equal(DbStatusCodes.AuthType.PasswordHash, data.GetProperty("credentialAuthType").GetString());
        Assert.True(data.GetProperty("hasActiveCredential").GetBoolean());
        Assert.True(data.GetProperty("hasApprovedSettings").GetBoolean());
        Assert.False(data.TryGetProperty("password", out _));
        Assert.False(data.TryGetProperty("verifier", out _));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var device = await db.Devices.SingleAsync(d => d.Id == deviceId);
        Assert.Equal(DbStatusCodes.DeviceLifecycle.Registered, device.LifecycleStatus);
        Assert.Equal(AuthApiFactory.AdminId, device.CreatedByUserId);

        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.Arm.Unknown, state.ArmState);
        Assert.Equal(DbStatusCodes.Siren.Unknown, state.SirenState);
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, state.HealthState);
        Assert.Equal(0, state.ConfirmedOwnershipVersion);
        Assert.Equal(0, state.AppliedSettingsVersion);

        var settings = await db.DeviceSettings.SingleAsync(s => s.DeviceId == deviceId);
        Assert.Equal(ApprovedDeviceSettings.TestDurationMs, settings.TestDurationMs);
        Assert.Equal(ApprovedDeviceSettings.HeartbeatTimeoutSeconds, settings.HeartbeatTimeoutSeconds);
        Assert.Equal(ApprovedDeviceSettings.CommandTimeoutSeconds, settings.CommandTimeoutSeconds);
        Assert.Equal(16, settings.EventBufferCapacity);
        Assert.Equal(45, settings.CommandTtlSeconds);
        Assert.Contains("schemaVersion", settings.AlarmRule, StringComparison.Ordinal);

        var credential = await db.DeviceCredentials.SingleAsync(c => c.DeviceId == deviceId);
        Assert.Equal(DbStatusCodes.AuthType.PasswordHash, credential.AuthType);
        Assert.Null(credential.RevokedAt);
        Assert.False(string.IsNullOrWhiteSpace(credential.Verifier));
        Assert.DoesNotContain(mqttPassword, credential.Verifier, StringComparison.Ordinal);
        var hasher = new PasswordHasher<object>();
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(new object(), credential.Verifier!, mqttPassword));

        var audit = await db.AuditLogs.SingleAsync(a =>
            a.Action == AuditActions.DeviceRegistered && a.TargetId == deviceId);
        Assert.Equal(DbStatusCodes.AuditResult.Success, audit.Result);
        Assert.Equal(AuthApiFactory.AdminId, audit.ActorUserId);
        Assert.Equal(deviceId, audit.DeviceId);
        Assert.DoesNotContain(mqttPassword, audit.Details ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("\"password\"", audit.Details ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"verifier\"", audit.Details ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PASSWORD_HASH", audit.Details ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duplicate_HardwareUid_Is_Conflict()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var body = NewRegistrationBody("hw-uid-dup-001");

        var first = await adminClient.PostAsJsonAsync("/api/admin/devices", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await adminClient.PostAsJsonAsync("/api/admin/devices", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var json = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DUPLICATE_HARDWARE_UID", json.GetProperty("error").GetProperty("code").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.Devices.CountAsync(d => d.HardwareUid == "hw-uid-dup-001"));
    }

    [Fact]
    public async Task Registration_Without_Approved_Settings_Is_Rejected_And_Atomic()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);

        var res = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid = "hw-uid-no-settings",
            displayName = "بدون إعدادات",
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash,
                password = "MqttSecret!99"
            },
            settings = new
            {
                alarmRule = new { schemaVersion = 1 },
                eventBufferCapacity = 0,
                commandTtlSeconds = 30
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_EVENT_BUFFER_CAPACITY", json.GetProperty("error").GetProperty("code").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Devices.AnyAsync(d => d.HardwareUid == "hw-uid-no-settings"));
        Assert.False(await db.DeviceCredentials.AnyAsync());
        Assert.False(await db.DeviceSettings.AnyAsync());
        Assert.False(await db.DeviceStates.AnyAsync());
    }

    [Fact]
    public async Task Registration_Without_Credential_Secret_Is_Rejected()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);

        var res = await adminClient.PostAsJsonAsync("/api/admin/devices", new
        {
            hardwareUid = "hw-uid-no-secret",
            displayName = "بدون سر",
            credential = new
            {
                authType = DbStatusCodes.AuthType.PasswordHash
            },
            settings = new
            {
                alarmRule = new { schemaVersion = 1 },
                eventBufferCapacity = 8,
                commandTtlSeconds = 20
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_DEVICE_PASSWORD", json.GetProperty("error").GetProperty("code").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Devices.AnyAsync(d => d.HardwareUid == "hw-uid-no-secret"));
    }

    [Fact]
    public async Task Owner_Is_Forbidden_On_Device_Registration()
    {
        using var ownerClient = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var res = await ownerClient.PostAsJsonAsync("/api/admin/devices", NewRegistrationBody("hw-uid-owner-forbid"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Registration_Is_401()
    {
        var res = await _client.PostAsJsonAsync("/api/admin/devices", NewRegistrationBody("hw-uid-anon"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
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

    private static object NewRegistrationBody(string hardwareUid) => new
    {
        hardwareUid,
        displayName = "جهاز",
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
    };
}
