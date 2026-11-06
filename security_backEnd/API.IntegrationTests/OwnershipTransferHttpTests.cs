using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Notifications;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

/// <summary>
/// BE-T-07: transfer/unlink, ownership gates, history isolation, and cross T-06 notification cancel.
/// Draft contracts only — does not adopt G1 or enable live MQTT/FCM.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class OwnershipTransferHttpTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public OwnershipTransferHttpTests()
    {
        _factory = new AuthApiFactory(services =>
        {
            services.AddSingleton<IMqttOutboundPublisher>(_publisher);
        });
    }

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
    public async Task Transfer_Ends_Prior_Ownership_Activates_New_And_Queues_OwnershipSync()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-basic-01", "نقل أساسي");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_basic");

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(transfer);
        Assert.Equal(DbStatusCodes.Ownership.Active, data.GetProperty("ownershipStatus").GetString());
        Assert.Equal(DbStatusCodes.Ownership.Ended, data.GetProperty("previousOwnershipStatus").GetString());
        Assert.Equal(2, data.GetProperty("ownershipVersion").GetInt64());
        Assert.Equal(1, data.GetProperty("confirmedOwnershipVersion").GetInt64());
        Assert.True(data.GetProperty("awaitingOwnershipAck").GetBoolean());
        Assert.False(data.GetProperty("canPublishCommands").GetBoolean());
        Assert.True(data.GetProperty("ownershipSyncQueued").GetBoolean());
        Assert.Equal(newOwnerId, data.GetProperty("ownerUserId").GetGuid());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var open = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId && o.EndedAt == null);
        Assert.Equal(newOwnerId, open.OwnerUserId);
        Assert.Equal(DbStatusCodes.Ownership.Active, open.Status);
        Assert.NotNull(open.ActivatedAt);

        var prior = await db.DeviceOwnerships.SingleAsync(o => o.Id == data.GetProperty("previousOwnershipId").GetGuid());
        Assert.Equal(DbStatusCodes.Ownership.Ended, prior.Status);
        Assert.NotNull(prior.EndedAt);

        var sync = await db.OutboxMessages.SingleAsync(m =>
            m.MessageType == OwnershipSyncDraft.OutboxPublishMessageType
            && m.AggregateId == open.Id);
        using var doc = JsonDocument.Parse(sync.Payload);
        Assert.Equal(2, doc.RootElement.GetProperty("ownershipVersion").GetInt64());
        Assert.True(doc.RootElement.GetProperty("isLinked").GetBoolean());
    }

    [Fact]
    public async Task Transfer_Cancels_Prior_Alarm_Notifications_But_Keeps_AdminAlert()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-notif-01", "إشعار نقل");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_notif");

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1);
        Assert.True(ingest.NotificationCreated);
        var alarmNotificationId = ingest.NotificationId!.Value;

        Guid adminAlertNotificationId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alertId = Guid.NewGuid();
            db.AdminAlerts.Add(new AdminAlert
            {
                Id = alertId,
                SenderUserId = AuthApiFactory.AdminId,
                RecipientUserId = AuthApiFactory.OwnerId,
                Title = "تنبيه إداري مستقل",
                Body = "يجب أن يبقى بعد النقل",
                CreatedAt = DateTimeOffset.UtcNow
            });
            adminAlertNotificationId = Guid.NewGuid();
            db.Notifications.Add(new Notification
            {
                Id = adminAlertNotificationId,
                RecipientUserId = AuthApiFactory.OwnerId,
                NotificationType = DbStatusCodes.NotificationType.AdminAlert,
                AdminAlertId = alertId,
                Status = DbStatusCodes.NotificationStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(transfer);
        Assert.True(data.GetProperty("cancelledNotifications").GetInt32() >= 1);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var alarmNotif = await db.Notifications.SingleAsync(n => n.Id == alarmNotificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Cancelled, alarmNotif.Status);
            Assert.Equal(
                EventPersistedDraft.CancellationReasons.OwnershipTransferred,
                alarmNotif.CancellationReason);
            Assert.NotNull(alarmNotif.CancelledAt);

            var adminNotif = await db.Notifications.SingleAsync(n => n.Id == adminAlertNotificationId);
            Assert.Equal(DbStatusCodes.NotificationStatus.Queued, adminNotif.Status);
            Assert.Null(adminNotif.CancelledAt);
        }

        // Guard still reports ADMIN_ALERT eligible and ALARM cancelled.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var guard = scope.ServiceProvider.GetRequiredService<INotificationRecipientGuard>();
            Assert.False(await guard.EnsureRecipientStillEligibleAsync(alarmNotificationId));
            Assert.True(await guard.EnsureRecipientStillEligibleAsync(adminAlertNotificationId));
        }
    }

    [Fact]
    public async Task Prior_Owner_Loses_Access_Immediately_New_Owner_Sees_Device_Without_Prior_History()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-hist-01", "عزل سجل");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_hist");

        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, ownershipVersion: 1);
        Assert.NotNull(ingest.AlarmEventId);
        var priorAlarmId = ingest.AlarmEventId!.Value;

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var priorCmd = await priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, priorCmd.StatusCode);
        var priorCommandId = (await AuthApiFactory.ReadDataAsync(priorCmd)).GetProperty("commandId").GetGuid();

        var priorAlarmsBefore = await priorOwner.GetAsync($"/api/owner/devices/{deviceId}/alarm-events");
        Assert.Equal(HttpStatusCode.OK, priorAlarmsBefore.StatusCode);
        Assert.NotEmpty((await AuthApiFactory.ReadDataAsync(priorAlarmsBefore)).EnumerateArray());

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        // Prior owner loses device API immediately.
        Assert.Equal(HttpStatusCode.NotFound, (await priorOwner.GetAsync($"/api/owner/devices/{deviceId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await priorOwner.GetAsync($"/api/owner/devices/{deviceId}/alarm-events")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await priorOwner.GetAsync($"/api/owner/devices/{deviceId}/commands")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
            {
                type = DbStatusCodes.CommandType.Disarm,
                idempotencyKey = Guid.NewGuid()
            })).StatusCode);

        using var newOwner = await CreateAuthorizedClientAsync("auth_owner_xfer_hist");
        var detail = await newOwner.GetAsync($"/api/owner/devices/{deviceId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailData = await AuthApiFactory.ReadDataAsync(detail);
        Assert.Equal(DbStatusCodes.Ownership.Active, detailData.GetProperty("ownershipStatus").GetString());
        Assert.True(detailData.GetProperty("awaitingOwnershipAck").GetBoolean());

        var alarms = await AuthApiFactory.ReadDataAsync(
            await newOwner.GetAsync($"/api/owner/devices/{deviceId}/alarm-events"));
        Assert.Empty(alarms.EnumerateArray());

        var commands = await AuthApiFactory.ReadDataAsync(
            await newOwner.GetAsync($"/api/owner/devices/{deviceId}/commands"));
        Assert.Empty(commands.EnumerateArray());

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await newOwner.GetAsync($"/api/owner/devices/{deviceId}/alarm-events/{priorAlarmId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await newOwner.GetAsync($"/api/owner/devices/{deviceId}/commands/{priorCommandId}")).StatusCode);

        // History rows remain on the device identity (no hard delete).
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.AlarmEvents.AnyAsync(e => e.Id == priorAlarmId));
        Assert.Equal(
            DbStatusCodes.CommandStatus.Cancelled,
            (await db.DeviceCommands.SingleAsync(c => c.Id == priorCommandId)).Status);
    }

    [Fact]
    public async Task New_Owner_Online_May_Accept_Command_But_Outbox_Holds_Until_Ownership_Ack()
    {
        _publisher.IsLivePublishEnabled = true;
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-cmd-01", "أمر بعد نقل");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_cmd");

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        using var newOwner = await CreateAuthorizedClientAsync("auth_owner_xfer_cmd");
        var accepted = await newOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var acceptedData = await AuthApiFactory.ReadDataAsync(accepted);
        var commandId = acceptedData.GetProperty("commandId").GetGuid();
        Assert.Equal(2, acceptedData.GetProperty("ownershipVersion").GetInt64());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingCommandsAsync();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m =>
                m.MessageType == ExecuteCommandDraft.OutboxPublishMessageType
                && m.AggregateId == commandId);
            Assert.Null(outbox.ProcessedAt);
            Assert.Equal(ExecuteCommandDraft.RejectCodes.OwnershipVersionStale, outbox.LastErrorCode);
            Assert.Empty(_publisher.Messages);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
            var confirm = await ownerships.ConfirmOwnershipVersionAsync(deviceId, 2);
            Assert.False(confirm.Activated); // already ACTIVE from transfer
            Assert.Equal(2, confirm.ConfirmedOwnershipVersion);

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
            outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingCommandsAsync();
            Assert.Single(_publisher.Messages);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
            Assert.NotNull(outbox.ProcessedAt);
        }
    }

    [Fact]
    public async Task New_Owner_Offline_Is_Rejected_For_Instant_Control()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-off-01", "غير متصل");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_off");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.LastSeenAt = DateTimeOffset.UtcNow.AddHours(-2);
            await db.SaveChangesAsync();
        }

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        using var newOwner = await CreateAuthorizedClientAsync("auth_owner_xfer_off");
        var rejected = await newOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task Concurrent_Transfer_And_Command_Serialize_On_PostgreSQL()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-race-01", "سباق");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_race");

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);

        var commandTask = priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        var transferTask = adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });

        await Task.WhenAll(commandTask, transferTask);
        var commandResponse = await commandTask;
        var transferResponse = await transferTask;

        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        Assert.True(
            commandResponse.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.NotFound,
            $"Unexpected command status: {commandResponse.StatusCode}");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var open = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId && o.EndedAt == null);
        Assert.Equal(newOwnerId, open.OwnerUserId);

        var pendingOrCancelled = await db.DeviceCommands
            .Where(c => c.DeviceId == deviceId)
            .ToListAsync();
        foreach (var command in pendingOrCancelled.Where(c => c.OwnershipId != open.Id))
        {
            Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);
        }

        // Outbox must not publish a prior-ownership command after transfer.
        _publisher.IsLivePublishEnabled = true;
        var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        await dispatch.DispatchPendingCommandsAsync();
        foreach (var msg in _publisher.Messages)
        {
            using var doc = JsonDocument.Parse(msg.PayloadJson);
            Assert.Equal(2, doc.RootElement.GetProperty("ownershipVersion").GetInt64());
        }
    }

    [Fact]
    public async Task After_Transfer_Commit_Outbox_Cannot_Publish_Ended_Ownership_Command()
    {
        _publisher.IsLivePublishEnabled = true;
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-outbox-post-01", "بعد النقل");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_outbox_post");

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var accepted = await priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var commandId = (await AuthApiFactory.ReadDataAsync(accepted)).GetProperty("commandId").GetGuid();

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);
        Assert.Equal(ExecuteCommandDraft.RejectCodes.OwnershipTransferred, command.RejectCode);

        // Even if an operator accelerates AvailableAt, dispatch must not publish ended ownership.
        var outbox = await db.OutboxMessages.SingleAsync(m =>
            m.MessageType == ExecuteCommandDraft.OutboxPublishMessageType
            && m.AggregateId == commandId);
        outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        outbox.ProcessedAt = null;
        outbox.LastErrorCode = null;
        await db.SaveChangesAsync();

        var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        await dispatch.DispatchPendingCommandsAsync();

        Assert.Empty(_publisher.Messages);
        await db.Entry(outbox).ReloadAsync();
        Assert.NotNull(outbox.ProcessedAt);
        // Terminal CANCELLED path parks the outbox without publishing; reject code may be null here.
        await db.Entry(command).ReloadAsync();
        Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);
    }

    [Fact]
    public async Task Outbox_Publish_Holds_Ownership_Locks_So_Transfer_Waits_Until_Dispatch_Commits()
    {
        _publisher.IsLivePublishEnabled = true;
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-outbox-lock-01", "قفل نشر");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_outbox_lock");

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var accepted = await priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Arm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var commandId = (await AuthApiFactory.ReadDataAsync(accepted)).GetProperty("commandId").GetGuid();

        _publisher.BlockNextPublish();
        var dispatchTask = Task.Run(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            return await dispatch.DispatchPendingCommandsAsync();
        });

        using var enterCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await _publisher.WaitUntilPublishEnteredAsync(enterCts.Token);

        var transferTask = adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });

        // Transfer must block on state/ownership locks held by the publishing dispatch TX.
        await Task.Delay(400);
        Assert.False(transferTask.IsCompleted);

        _publisher.ReleaseBlockedPublish();
        var claimed = await dispatchTask;
        Assert.True(claimed >= 1);

        var transferResponse = await transferTask;
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);

        // Publish was allowed only while prior ownership was still open under FOR UPDATE.
        Assert.Single(_publisher.Messages);
        using (var doc = JsonDocument.Parse(_publisher.Messages[0].PayloadJson))
        {
            Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());
            Assert.Equal(1, doc.RootElement.GetProperty("ownershipVersion").GetInt64());
        }

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);
        Assert.NotNull(command.SentAt);
        Assert.Equal(ExecuteCommandDraft.RejectCodes.OwnershipTransferred, command.RejectCode);

        // No further publish of the ended-ownership command after transfer Commit.
        var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
        outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        outbox.ProcessedAt = null;
        await db.SaveChangesAsync();
        var dispatch = verify.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        await dispatch.DispatchPendingCommandsAsync();
        Assert.Single(_publisher.Messages);
    }

    [Fact]
    public async Task Concurrent_Dispatch_After_Transfer_Never_Publishes_Prior_Ownership_Version()
    {
        _publisher.IsLivePublishEnabled = true;
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-outbox-race-01", "سباق نشر");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_outbox_race");

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var accepted = await priorOwner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Disarm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var commandId = (await AuthApiFactory.ReadDataAsync(accepted)).GetProperty("commandId").GetGuid();

        var transferTask = adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        var dispatchTask = Task.Run(async () =>
        {
            // Small stagger so both hit PostgreSQL around the same window.
            await Task.Delay(Random.Shared.Next(0, 30));
            await using var scope = _factory.Services.CreateAsyncScope();
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            return await dispatch.DispatchPendingCommandsAsync();
        });

        await Task.WhenAll(transferTask, dispatchTask);
        Assert.Equal(HttpStatusCode.OK, (await transferTask).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var command = await db.DeviceCommands.SingleAsync(c => c.Id == commandId);
        Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);

        // Any published payload must belong to ownership still open at publish lock time (version 1).
        // After transfer Commit, a second dispatch must not emit another message.
        foreach (var msg in _publisher.Messages)
        {
            using var doc = JsonDocument.Parse(msg.PayloadJson);
            Assert.Equal(1, doc.RootElement.GetProperty("ownershipVersion").GetInt64());
            Assert.Equal(commandId, doc.RootElement.GetProperty("commandId").GetGuid());
        }

        var publishedCount = _publisher.Messages.Count;
        var outbox = await db.OutboxMessages.SingleAsync(m => m.AggregateId == commandId);
        outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        if (outbox.ProcessedAt is not null)
        {
            // Force a retry attempt after Commit to prove refusal.
            outbox.ProcessedAt = null;
        }

        await db.SaveChangesAsync();
        var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
        await dispatch.DispatchPendingCommandsAsync();
        Assert.Equal(publishedCount, _publisher.Messages.Count);

        await db.Entry(command).ReloadAsync();
        Assert.Equal(DbStatusCodes.CommandStatus.Cancelled, command.Status);
        await db.Entry(outbox).ReloadAsync();
        Assert.NotNull(outbox.ProcessedAt);
        // publishedCount may be 0 (transfer won) or 1 (dispatch published under pre-commit ownership locks).
        Assert.True(publishedCount is 0 or 1);
    }

    [Fact]
    public async Task Unlink_Ends_Ownership_Cancels_Pending_And_Queues_Unlink_Sync()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-unlink-01", "إلغاء ربط");

        using var owner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var cmd = await owner.PostAsJsonAsync($"/api/owner/devices/{deviceId}/commands", new
        {
            type = DbStatusCodes.CommandType.Disarm,
            idempotencyKey = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Accepted, cmd.StatusCode);

        var unlink = await adminClient.PostAsync($"/api/admin/devices/{deviceId}/unlink", null);
        Assert.Equal(HttpStatusCode.OK, unlink.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(unlink);
        Assert.Equal(DbStatusCodes.Ownership.Ended, data.GetProperty("closedOwnershipStatus").GetString());
        Assert.True(data.GetProperty("cancelledCommands").GetInt32() >= 1);
        Assert.True(data.GetProperty("ownershipSyncQueued").GetBoolean());

        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/owner/devices/{deviceId}")).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.DeviceOwnerships.AnyAsync(o => o.DeviceId == deviceId && o.EndedAt == null));
        var sync = await db.OutboxMessages
            .Where(m => m.MessageType == OwnershipSyncDraft.OutboxPublishMessageType)
            .OrderByDescending(m => m.CreatedAt)
            .FirstAsync();
        using var doc = JsonDocument.Parse(sync.Payload);
        Assert.False(doc.RootElement.GetProperty("isLinked").GetBoolean());
    }

    [Fact]
    public async Task New_Owner_Alarm_Does_Not_Leak_To_Prior_Owner_Notifications()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAndOnlineAsync(adminClient, "hw-xfer-leak-01", "تسرب");
        var newOwnerId = await CreateExtraOwnerAsync("auth_owner_xfer_leak");

        var transfer = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/transfer", new
        {
            newOwnerUserId = newOwnerId
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var ownerships = scope.ServiceProvider.GetRequiredService<IDeviceOwnershipService>();
            await ownerships.ConfirmOwnershipVersionAsync(deviceId, 2);
        }

        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 2);
        Assert.True(ingest.NotificationCreated);

        using var priorOwner = await CreateAuthorizedClientAsync(AuthApiFactory.OwnerUserName);
        var priorNotifs = await AuthApiFactory.ReadDataAsync(await priorOwner.GetAsync("/api/owner/notifications"));
        Assert.DoesNotContain(
            priorNotifs.EnumerateArray(),
            n => n.GetProperty("alarmEventId").ValueKind != JsonValueKind.Null
                 && n.GetProperty("alarmEventId").GetGuid() == ingest.AlarmEventId);

        using var newOwner = await CreateAuthorizedClientAsync("auth_owner_xfer_leak");
        var newAlarms = await AuthApiFactory.ReadDataAsync(
            await newOwner.GetAsync($"/api/owner/devices/{deviceId}/alarm-events"));
        Assert.Contains(
            newAlarms.EnumerateArray(),
            e => e.GetProperty("alarmEventId").GetGuid() == ingest.AlarmEventId);
    }

    private async Task<Guid> RegisterActivateAndOnlineAsync(
        HttpClient adminClient,
        string hardwareUid,
        string displayName)
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
        Assert.True((await ownerships.ConfirmOwnershipVersionAsync(deviceId, ownershipVersion)).Activated);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
        state.LastSeenAt = DateTimeOffset.UtcNow;
        state.ArmState = DbStatusCodes.Arm.Disarmed;
        state.SirenState = DbStatusCodes.Siren.Off;
        await db.SaveChangesAsync();
        return deviceId;
    }

    private async Task<AlarmEventIngestResultDto> IngestAlarmAsync(
        Guid deviceId,
        Guid eventUid,
        long ownershipVersion)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAlarmEventIngestService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = deviceId,
            AuthenticatedDeviceId = deviceId,
            Topic = AlarmEventDraft.Topics.Events(deviceId),
            PayloadJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = AlarmEventDraft.MessageTypes.AlarmEvent,
                bootId = "boot-xfer",
                eventUid,
                ownershipVersion,
                sensorKey = "vibration-1",
                wasBuffered = false,
                clockTrusted = false
            })
        });
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

    private static async Task<Guid> RegisterDeviceAsync(
        HttpClient adminClient,
        string hardwareUid,
        string displayName)
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
