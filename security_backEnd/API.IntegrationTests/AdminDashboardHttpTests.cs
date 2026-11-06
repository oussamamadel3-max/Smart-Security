using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Events;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

/// <summary>
/// BE-T-08 / SS-P7-010..030 acceptance on PostgreSQL (draft contracts; FCM not enabled).
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class AdminDashboardHttpTests : IAsyncLifetime
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
    public async Task Unauthorized_And_Owner_Are_Rejected_From_Admin_Dashboard()
    {
        var anonOwners = await _client.GetAsync("/api/admin/owners");
        Assert.Equal(HttpStatusCode.Unauthorized, anonOwners.StatusCode);

        var anonDevices = await _client.GetAsync("/api/admin/devices");
        Assert.Equal(HttpStatusCode.Unauthorized, anonDevices.StatusCode);

        var anonAudit = await _client.GetAsync("/api/admin/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, anonAudit.StatusCode);

        var anonAlerts = await _client.GetAsync("/api/admin/alerts");
        Assert.Equal(HttpStatusCode.Unauthorized, anonAlerts.StatusCode);

        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/owners")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/alerts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/admin/alerts", new
        {
            recipientUserId = AuthApiFactory.OwnerId,
            title = "ممنوع",
            body = "المالك لا يرسل تنبيهاً إدارياً"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/device-faults")).StatusCode);
    }

    [Fact]
    public async Task Offline_Device_Shows_Last_Known_State_And_Unknown_Health_Without_Diagnosis()
    {
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkAndActivateAsync(admin, "hw-admin-overview-01", "أدمن عرض");

        await IngestStateAsync(
            deviceId,
            stateSequence: 3,
            arm: DbStatusCodes.Arm.Armed,
            siren: DbStatusCodes.Siren.Off,
            health: DbStatusCodes.Diagnostic.Unknown,
            confirmedOwnershipVersion: 1);

        var lastStateAt = DateTimeOffset.UtcNow.AddMinutes(-90);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow.AddHours(-2);
            state.LastStateAt = lastStateAt;
            await db.SaveChangesAsync();
        }

        var list = await admin.GetAsync("/api/admin/devices");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = await AuthApiFactory.ReadDataAsync(list);
        var row = items.EnumerateArray().Single(d => d.GetProperty("deviceId").GetGuid() == deviceId);
        var indicators = row.GetProperty("indicators");

        Assert.Equal(MqttInboundDraft.ConnectionStatus.Offline, indicators.GetProperty("connectionStatus").GetString());
        Assert.Equal(DbStatusCodes.Arm.Armed, indicators.GetProperty("armState").GetString());
        Assert.Equal(DbStatusCodes.Siren.Off, indicators.GetProperty("sirenState").GetString());
        Assert.Equal(DbStatusCodes.Diagnostic.Unknown, indicators.GetProperty("healthState").GetString());
        Assert.NotEqual(DbStatusCodes.Diagnostic.Healthy, indicators.GetProperty("healthState").GetString());
        Assert.Equal(lastStateAt.ToUnixTimeMilliseconds(),
            indicators.GetProperty("lastStateAt").GetDateTimeOffset().ToUnixTimeMilliseconds());

        // Offline must not invent a hardware fault row.
        var faults = await admin.GetAsync($"/api/admin/device-faults?deviceId={deviceId}");
        Assert.Equal(HttpStatusCode.OK, faults.StatusCode);
        var faultPage = await AuthApiFactory.ReadDataAsync(faults);
        Assert.Equal(0, faultPage.GetProperty("totalCount").GetInt64());
        Assert.Equal(0, faultPage.GetProperty("items").GetArrayLength());

        var owners = await admin.GetAsync("/api/admin/owners");
        Assert.Equal(HttpStatusCode.OK, owners.StatusCode);
        var ownerList = await AuthApiFactory.ReadDataAsync(owners);
        Assert.Contains(ownerList.EnumerateArray(), o => o.GetProperty("id").GetGuid() == AuthApiFactory.OwnerId);
    }

    [Fact]
    public async Task Audit_Logs_Are_Readable_With_Filter_And_Have_No_Mutation_Routes()
    {
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        await RegisterDeviceAsync(admin, "hw-admin-audit-01", "تدقيق");

        var filtered = await admin.GetAsync(
            $"/api/admin/audit-logs?action={AuditActions.DeviceRegistered}&page=1&pageSize=10&sortBy=occurredAt&sortDir=desc");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        var page = await AuthApiFactory.ReadDataAsync(filtered);
        Assert.True(page.GetProperty("totalCount").GetInt64() >= 1);
        var first = page.GetProperty("items")[0];
        Assert.Equal(AuditActions.DeviceRegistered, first.GetProperty("action").GetString());
        Assert.Equal(DbStatusCodes.AuditResult.Success, first.GetProperty("result").GetString());

        // Read-only surface: mutation verbs must not succeed.
        var putStatus = (await admin.PutAsJsonAsync("/api/admin/audit-logs", new { })).StatusCode;
        var deleteStatus = (await admin.DeleteAsync("/api/admin/audit-logs")).StatusCode;
        Assert.True(putStatus is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound);
        Assert.True(deleteStatus is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_Alert_Persists_As_Pending_Without_Fake_Fcm_Success_And_Survives_Failed_Delivery_Plus_Transfer()
    {
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkAndActivateAsync(admin, "hw-admin-alert-01", "تنبيه");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_admin_alert_xfer");

        var create = await admin.PostAsJsonAsync("/api/admin/alerts", new
        {
            recipientUserId = AuthApiFactory.OwnerId,
            title = "تنبيه مراجعة",
            body = "يبقى محفوظاً عند فشل التسليم"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await AuthApiFactory.ReadDataAsync(create);
        var alertId = created.GetProperty("adminAlertId").GetGuid();
        var notificationId = created.GetProperty("notificationId").GetGuid();
        Assert.Equal(DbStatusCodes.NotificationStatus.Queued, created.GetProperty("notificationStatus").GetString());
        Assert.Equal(AdminAlertDeliveryCodes.Pending, created.GetProperty("deliveryAttemptStatus").GetString());
        Assert.False(created.GetProperty("deliveryAttempted").GetBoolean());
        Assert.NotEqual(AdminAlertDeliveryCodes.Accepted, created.GetProperty("deliveryAttemptStatus").GetString());

        // Simulate a real failed delivery attempt (as T-09 would record) — alert stays for review.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = await db.Notifications.SingleAsync(n => n.Id == notificationId);
            notification.Status = DbStatusCodes.NotificationStatus.Failed;
            notification.LastAttemptAt = DateTimeOffset.UtcNow;
            notification.NextAttemptAt = null;
            await db.SaveChangesAsync();
        }

        var getFailed = await admin.GetAsync($"/api/admin/alerts/{alertId}");
        Assert.Equal(HttpStatusCode.OK, getFailed.StatusCode);
        var failedView = await AuthApiFactory.ReadDataAsync(getFailed);
        Assert.Equal(DbStatusCodes.NotificationStatus.Failed, failedView.GetProperty("notificationStatus").GetString());
        Assert.Equal(AdminAlertDeliveryCodes.Failed, failedView.GetProperty("deliveryAttemptStatus").GetString());
        Assert.True(failedView.GetProperty("deliveryAttempted").GetBoolean());

        // FAILED row without last_attempt_at must not be presented as a confirmed send failure.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = await db.Notifications.SingleAsync(n => n.Id == notificationId);
            notification.Status = DbStatusCodes.NotificationStatus.Failed;
            notification.LastAttemptAt = null;
            await db.SaveChangesAsync();
        }

        var inconsistent = await AuthApiFactory.ReadDataAsync(await admin.GetAsync($"/api/admin/alerts/{alertId}"));
        Assert.Equal(DbStatusCodes.NotificationStatus.Failed, inconsistent.GetProperty("notificationStatus").GetString());
        Assert.Equal(AdminAlertDeliveryCodes.Pending, inconsistent.GetProperty("deliveryAttemptStatus").GetString());
        Assert.False(inconsistent.GetProperty("deliveryAttempted").GetBoolean());
        Assert.NotEqual(AdminAlertDeliveryCodes.Accepted, inconsistent.GetProperty("deliveryAttemptStatus").GetString());

        // Reset to QUEUED then transfer ownership — ADMIN_ALERT must not cancel.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = await db.Notifications.SingleAsync(n => n.Id == notificationId);
            notification.Status = DbStatusCodes.NotificationStatus.Queued;
            notification.LastAttemptAt = null;
            notification.NextAttemptAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        await ForceOnlineAsync(deviceId);
        var transfer = await admin.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = await db.Notifications.SingleAsync(n => n.Id == notificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Queued, notification.Status);
            Assert.Null(notification.CancelledAt);
            Assert.True(await db.AdminAlerts.AnyAsync(a => a.Id == alertId));
        }

        var afterTransfer = await admin.GetAsync($"/api/admin/alerts/{alertId}");
        Assert.Equal(HttpStatusCode.OK, afterTransfer.StatusCode);
        var after = await AuthApiFactory.ReadDataAsync(afterTransfer);
        Assert.Equal(AdminAlertDeliveryCodes.Pending, after.GetProperty("deliveryAttemptStatus").GetString());
    }

    [Fact]
    public async Task Device_Fault_List_Shows_Only_Reported_Faults_Not_Offline()
    {
        using var admin = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterLinkAndActivateAsync(admin, "hw-admin-fault-01", "عطل");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow.AddHours(-3);
            state.HealthState = DbStatusCodes.Diagnostic.Unknown;
            await db.SaveChangesAsync();
        }

        var empty = await admin.GetAsync($"/api/admin/device-faults?deviceId={deviceId}");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal(0, (await AuthApiFactory.ReadDataAsync(empty)).GetProperty("totalCount").GetInt64());

        var faultUid = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId && o.EndedAt == null);
            db.DeviceFaults.Add(new DeviceFault
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                FaultUid = faultUid,
                OwnershipId = ownership.Id,
                FaultCode = "SENSOR_READ_FAIL",
                Status = DbStatusCodes.Fault.Active,
                ReceivedAt = DateTimeOffset.UtcNow,
                OwnershipVersion = ownership.Version,
                Details = "{\"source\":\"device\"}"
            });
            await db.SaveChangesAsync();
        }

        var listed = await admin.GetAsync($"/api/admin/device-faults?deviceId={deviceId}");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var page = await AuthApiFactory.ReadDataAsync(listed);
        Assert.Equal(1, page.GetProperty("totalCount").GetInt64());
        Assert.Equal(faultUid, page.GetProperty("items")[0].GetProperty("faultUid").GetGuid());
        Assert.Equal("SENSOR_READ_FAIL", page.GetProperty("items")[0].GetProperty("faultCode").GetString());
    }

    private async Task ForceOnlineAsync(Guid deviceId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        state.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task IngestStateAsync(
        Guid deviceId,
        long stateSequence,
        string arm,
        string siren,
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
                bootId = "boot-admin-dashboard",
                stateSequence,
                confirmedOwnershipVersion,
                armState = arm,
                sirenState = siren,
                healthState = health,
                currentAlarmEventUid = (Guid?)null,
                clockTrusted = false
            })
        });
        Assert.Equal(MqttInboundDraft.IngestOutcomes.Applied, result.Outcome);
    }

    private async Task<Guid> RegisterLinkAndActivateAsync(HttpClient admin, string hardwareUid, string displayName)
    {
        var deviceId = await RegisterDeviceAsync(admin, hardwareUid, displayName);
        var link = await admin.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var ownershipVersion = (await AuthApiFactory.ReadDataAsync(link)).GetProperty("ownershipVersion").GetInt64();

        await using var scope = _factory.Services.CreateAsyncScope();
        var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
        Assert.True((await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion)).Activated);
        return deviceId;
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
