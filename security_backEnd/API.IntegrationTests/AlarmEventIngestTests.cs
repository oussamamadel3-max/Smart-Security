using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Application.Dtos.MqttDtos;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Devices;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class AlarmEventIngestTests : IAsyncLifetime
{
    private readonly RecordingMqttOutboundPublisher _publisher = new();
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public AlarmEventIngestTests()
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
    public async Task Valid_Alarm_Event_Persists_With_Notification_And_EventPersisted_Outbox()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-ok-01", "حادثة");
        var eventUid = Guid.NewGuid();

        var result = await IngestAlarmAsync(
            deviceId,
            eventUid,
            ownershipVersion: 1,
            wasBuffered: false,
            clockTrusted: true,
            occurredDeviceAt: DateTimeOffset.Parse("2026-11-06T10:00:00Z"));

        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, result.Outcome);
        Assert.True(result.EventCreated);
        Assert.True(result.NotificationCreated);
        Assert.True(result.EventPersistedQueued);
        Assert.NotNull(result.AlarmEventId);
        Assert.NotNull(result.NotificationId);
        Assert.NotNull(result.OutboxMessageId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alarm = await db.AlarmEvents.SingleAsync(e => e.DeviceId == deviceId && e.EventUid == eventUid);
        Assert.Equal(DbStatusCodes.AlarmEvent.Active, alarm.Status);
        Assert.False(alarm.WasBuffered);
        Assert.True(alarm.ClockTrusted);
        Assert.Equal(DateTimeOffset.Parse("2026-11-06T10:00:00Z"), alarm.OccurredDeviceAt);
        Assert.NotEqual(default, alarm.ReceivedAt);
        Assert.NotNull(alarm.OwnershipId);

        var notifications = await db.Notifications.Where(n => n.AlarmEventId == alarm.Id).ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(AuthApiFactory.OwnerId, notifications[0].RecipientUserId);
        Assert.Equal(DbStatusCodes.NotificationType.Alarm, notifications[0].NotificationType);
        Assert.Equal(DbStatusCodes.NotificationStatus.Queued, notifications[0].Status);

        var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == result.OutboxMessageId);
        Assert.Equal(EventPersistedDraft.OutboxPublishMessageType, outbox.MessageType);
        Assert.Equal(EventPersistedDraft.OutboxAggregateType, outbox.AggregateType);
        Assert.Equal(alarm.Id, outbox.AggregateId);
        Assert.Null(outbox.ProcessedAt);
        using (var doc = JsonDocument.Parse(outbox.Payload))
        {
            Assert.Equal(EventPersistedDraft.MqttMessageType, doc.RootElement.GetProperty("messageType").GetString());
            Assert.Equal(eventUid, doc.RootElement.GetProperty("eventUid").GetGuid());
            Assert.True(doc.RootElement.GetProperty("stored").GetBoolean());
        }
    }

    [Fact]
    public async Task Duplicate_And_Concurrent_Same_Event_Yield_One_Alarm_One_Notification()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-dup-01", "تكرار");
        var eventUid = Guid.NewGuid();

        var first = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, first.Outcome);

        // Simulate prior successful EVENT_PERSISTED publish, then redelivery must re-queue same outbox row.
        await using (var markScope = _factory.Services.CreateAsyncScope())
        {
            var markDb = markScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var prior = await markDb.OutboxMessages.SingleAsync(m => m.Id == first.OutboxMessageId);
            prior.ProcessedAt = DateTimeOffset.UtcNow;
            prior.AttemptCount = 7;
            await markDb.SaveChangesAsync();
        }

        var second = await IngestAlarmAsync(deviceId, eventUid, 1, true, false, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, second.Outcome);
        Assert.False(second.EventCreated);
        Assert.False(second.NotificationCreated);
        Assert.True(second.EventPersistedQueued);
        Assert.Equal(first.OutboxMessageId, second.OutboxMessageId);

        await using (var checkScope = _factory.Services.CreateAsyncScope())
        {
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reopened = await checkDb.OutboxMessages.SingleAsync(m => m.Id == first.OutboxMessageId);
            Assert.Null(reopened.ProcessedAt);
            Assert.Equal(0, reopened.AttemptCount);
        }

        var concurrentPayloads = Enumerable.Range(0, 8)
            .Select(_ => BuildAlarmPayload(deviceId, eventUid, 1, true, false, null))
            .ToList();
        var tasks = concurrentPayloads.Select(async payload =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var ingest = scope.ServiceProvider.GetRequiredService<IAlarmEventIngestService>();
            return await ingest.IngestAsync(new DeviceInboundEnvelope
            {
                TopicDeviceId = deviceId,
                AuthenticatedDeviceId = deviceId,
                Topic = AlarmEventDraft.Topics.Events(deviceId),
                PayloadJson = payload
            });
        });
        var concurrent = await Task.WhenAll(tasks);
        Assert.All(concurrent, r =>
            Assert.True(
                r.Outcome is AlarmEventDraft.IngestOutcomes.DuplicateConfirmed
                    or AlarmEventDraft.IngestOutcomes.Applied));

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AlarmEvents.CountAsync(e => e.DeviceId == deviceId && e.EventUid == eventUid));
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.AlarmEventId == first.AlarmEventId));
        // ux_outbox_messages_type_aggregate: one durable row re-queued on redelivery (not a second insert).
        var outbox = await db.OutboxMessages.SingleAsync(m =>
            m.MessageType == EventPersistedDraft.OutboxPublishMessageType
            && m.AggregateId == first.AlarmEventId);
        Assert.Null(outbox.ProcessedAt);
        Assert.True(outbox.AvailableAt <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Rejected_Or_Failed_Ingest_Does_Not_Create_Alarm_Notification_Or_Outbox()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-fail-01", "فشل");
        var otherId = await RegisterDeviceAsync(adminClient, "hw-alarm-fail-other", "آخر");

        var mismatch = await IngestRawAsync(
            topicDeviceId: deviceId,
            authenticatedDeviceId: otherId,
            topic: AlarmEventDraft.Topics.Events(deviceId),
            payload: BuildAlarmPayload(deviceId, Guid.NewGuid(), 1, false, false, null));
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Rejected, mismatch.Outcome);
        Assert.Equal("DEVICE_IDENTITY_MISMATCH", mismatch.RejectionCode);

        var invalid = await IngestRawAsync(
            deviceId,
            deviceId,
            AlarmEventDraft.Topics.Events(deviceId),
            """{"schemaVersion":1,"deviceId":"00000000-0000-0000-0000-000000000001","messageId":"00000000-0000-0000-0000-000000000002","messageType":"ALARM_EVENT","bootId":"x"}""");
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Rejected, invalid.Outcome);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.AlarmEvents.CountAsync(e => e.DeviceId == deviceId));
        Assert.Equal(0, await db.Notifications.CountAsync());
        Assert.Equal(0, await db.OutboxMessages.CountAsync(m =>
            m.MessageType == EventPersistedDraft.OutboxPublishMessageType));
    }

    [Fact]
    public async Task Pending_Ownership_Persists_Alarm_Without_Notification()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterDeviceAsync(adminClient, "hw-alarm-pending-01", "معلق");
        await LinkAsync(adminClient, deviceId);

        var result = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1, false, false, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, result.Outcome);
        Assert.True(result.EventCreated);
        Assert.False(result.NotificationCreated);
        Assert.True(result.EventPersistedQueued);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AlarmEvents.CountAsync(e => e.DeviceId == deviceId));
        Assert.Equal(0, await db.Notifications.CountAsync());
    }

    [Fact]
    public async Task Ended_Ownership_Version_Saves_Historically_Without_Notification()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-ended-01", "منتهية");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var old = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId);
            old.Status = DbStatusCodes.Ownership.Ended;
            old.EndedAt = DateTimeOffset.UtcNow;

            db.DeviceOwnerships.Add(new DeviceOwnership
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                OwnerUserId = AuthApiFactory.OwnerId,
                Version = 2,
                Status = DbStatusCodes.Ownership.Active,
                StartedAt = DateTimeOffset.UtcNow,
                ActivatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = AuthApiFactory.AdminId
            });
            var state = await db.DeviceStates.SingleAsync(s => s.DeviceId == deviceId);
            state.ConfirmedOwnershipVersion = 2;
            await db.SaveChangesAsync();
        }

        var result = await IngestAlarmAsync(deviceId, Guid.NewGuid(), ownershipVersion: 1, false, true, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, result.Outcome);
        Assert.True(result.EventCreated);
        Assert.False(result.NotificationCreated);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var alarm = await db2.AlarmEvents.SingleAsync(e => e.DeviceId == deviceId);
        Assert.NotNull(alarm.OwnershipId);
        Assert.Equal(1, alarm.OwnershipVersion);
        Assert.Equal(0, await db2.Notifications.CountAsync());
    }

    [Fact]
    public async Task WasBuffered_And_Untrusted_Clock_Preserve_Server_Receive_Time_Only()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-buf-01", "متأخر");
        var claimed = DateTimeOffset.Parse("2025-01-01T00:00:00Z");

        var before = DateTimeOffset.UtcNow;
        var result = await IngestAlarmAsync(
            deviceId,
            Guid.NewGuid(),
            1,
            wasBuffered: true,
            clockTrusted: false,
            occurredDeviceAt: claimed);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, result.Outcome);
        Assert.True(result.WasBuffered);
        Assert.False(result.ClockTrusted);
        Assert.Null(result.OccurredDeviceAt);
        Assert.NotNull(result.ReceivedAt);
        Assert.InRange(result.ReceivedAt!.Value, before.AddSeconds(-1), after.AddSeconds(1));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var alarm = await db.AlarmEvents.SingleAsync(e => e.Id == result.AlarmEventId);
        Assert.True(alarm.WasBuffered);
        Assert.False(alarm.ClockTrusted);
        Assert.Null(alarm.OccurredDeviceAt);
    }

    [Fact]
    public async Task EventPersisted_Outbox_Publishes_After_Commit_And_Retries_After_Worker_Failure()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-outbox-01", "تأكيد");
        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);
        Assert.True(ingest.EventPersistedQueued);

        _publisher.FailNext(1);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        Assert.Empty(_publisher.Messages);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
            Assert.Null(outbox.ProcessedAt);
            Assert.Equal(EventPersistedDraft.RejectCodes.PublishFailed, outbox.LastErrorCode);
            outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            var claimed = await dispatch.DispatchPendingEventPersistedAsync();
            Assert.True(claimed >= 1);
        }

        Assert.Single(_publisher.Messages);
        Assert.Equal(EventPersistedDraft.Topics.Commands(deviceId), _publisher.Messages[0].Topic);
        using (var doc = JsonDocument.Parse(_publisher.Messages[0].PayloadJson))
        {
            Assert.Equal(EventPersistedDraft.MqttMessageType, doc.RootElement.GetProperty("messageType").GetString());
            Assert.Equal(eventUid, doc.RootElement.GetProperty("eventUid").GetGuid());
            Assert.True(doc.RootElement.GetProperty("stored").GetBoolean());
        }

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var done = await db2.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
        Assert.NotNull(done.ProcessedAt);
    }

    [Fact]
    public async Task Redelivery_After_Successful_Publish_Reopens_Outbox_With_Fresh_Attempt_Budget()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-reopen-01", "إعادة فتح");
        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        Assert.Single(_publisher.Messages);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
            Assert.NotNull(outbox.ProcessedAt);
            outbox.AttemptCount = 9; // simulate prior retries before the successful cycle
            await db.SaveChangesAsync();
        }

        var redelivery = await IngestAlarmAsync(deviceId, eventUid, 1, true, false, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, redelivery.Outcome);
        Assert.False(redelivery.NotificationCreated);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var reopened = await db2.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
        Assert.Null(reopened.ProcessedAt);
        Assert.Equal(0, reopened.AttemptCount);
        Assert.Null(reopened.LastErrorCode);
        Assert.True(reopened.AvailableAt <= DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(1, await db2.Notifications.CountAsync(n => n.AlarmEventId == ingest.AlarmEventId));
    }

    [Fact]
    public async Task Redelivery_During_Outbox_Publish_Waits_Then_Requeues_Without_Second_Notification()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-race-01", "سباق عامل");
        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);

        _publisher.BlockNextPublish();
        var dispatchTask = Task.Run(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            return await dispatch.DispatchPendingEventPersistedAsync();
        });

        using var enterCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _publisher.WaitUntilPublishEnteredAsync(enterCts.Token);

        // Ingest must block on outbox FOR UPDATE until the worker commits ProcessedAt.
        var redeliveryTask = IngestAlarmAsync(deviceId, eventUid, 1, true, false, null);
        await Task.Delay(200);
        Assert.False(redeliveryTask.IsCompleted);

        _publisher.ReleaseBlockedPublish();
        var claimed = await dispatchTask;
        Assert.True(claimed >= 1);

        var redelivery = await redeliveryTask;
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, redelivery.Outcome);
        Assert.False(redelivery.NotificationCreated);
        Assert.Equal(ingest.OutboxMessageId, redelivery.OutboxMessageId);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.AlarmEvents.CountAsync(e => e.EventUid == eventUid));
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.AlarmEventId == ingest.AlarmEventId));
        var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
        Assert.Null(outbox.ProcessedAt);
        Assert.Equal(0, outbox.AttemptCount);
        Assert.Single(_publisher.Messages);
    }

    [Fact]
    public async Task Redelivery_After_Publish_Failure_Accelerates_Backoff_Without_Resetting_Attempts()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-fail-requeue-01", "فشل ثم إعادة");
        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);

        _publisher.FailNext(1);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        int attemptsAfterFail;
        DateTimeOffset availableAfterFail;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
            Assert.Null(outbox.ProcessedAt);
            Assert.Equal(EventPersistedDraft.RejectCodes.PublishFailed, outbox.LastErrorCode);
            Assert.True(outbox.AvailableAt > DateTimeOffset.UtcNow);
            attemptsAfterFail = outbox.AttemptCount;
            availableAfterFail = outbox.AvailableAt;
            Assert.True(attemptsAfterFail >= 1);
        }

        var redelivery = await IngestAlarmAsync(deviceId, eventUid, 1, true, false, null);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, redelivery.Outcome);

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var accelerated = await db2.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
        Assert.Null(accelerated.ProcessedAt);
        Assert.Equal(attemptsAfterFail, accelerated.AttemptCount);
        Assert.True(accelerated.AvailableAt < availableAfterFail);
        Assert.True(accelerated.AvailableAt <= DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(1, await db2.Notifications.CountAsync(n => n.AlarmEventId == ingest.AlarmEventId));
    }

    [Fact]
    public async Task Crash_After_Publish_Before_ProcessedAt_Allows_AtLeastOnce_Republish()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-crash-01", "تعطل بعد نشر");
        var eventUid = Guid.NewGuid();
        var ingest = await IngestAlarmAsync(deviceId, eventUid, 1, false, false, null);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        Assert.Single(_publisher.Messages);

        // Simulate worker crash after MQTT publish succeeded but before ProcessedAt commit/persist.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
            outbox.ProcessedAt = null;
            outbox.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dispatch = scope.ServiceProvider.GetRequiredService<IOutboxDispatchService>();
            await dispatch.DispatchPendingEventPersistedAsync();
        }

        Assert.Equal(2, _publisher.Messages.Count);
        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var done = await db2.OutboxMessages.SingleAsync(m => m.Id == ingest.OutboxMessageId);
        Assert.NotNull(done.ProcessedAt);
        Assert.Equal(1, await db2.Notifications.CountAsync(n => n.AlarmEventId == ingest.AlarmEventId));
    }

    [Fact]
    public async Task Recipient_Guard_Cancels_When_Ownership_No_Longer_Active_Before_Fcm()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-guard-01", "حراسة");
        var ingest = await IngestAlarmAsync(deviceId, Guid.NewGuid(), 1, false, false, null);
        Assert.True(ingest.NotificationCreated);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ownership = await db.DeviceOwnerships.SingleAsync(o => o.DeviceId == deviceId && o.Version == 1);
            ownership.Status = DbStatusCodes.Ownership.Ended;
            ownership.EndedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var guard = scope.ServiceProvider.GetRequiredService<INotificationRecipientGuard>();
            var eligible = await guard.EnsureRecipientStillEligibleAsync(ingest.NotificationId!.Value);
            Assert.False(eligible);
        }

        await using var verify = _factory.Services.CreateAsyncScope();
        var db2 = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var notification = await db2.Notifications.SingleAsync(n => n.Id == ingest.NotificationId);
        Assert.Equal(DbStatusCodes.NotificationStatus.Cancelled, notification.Status);
        Assert.Equal(EventPersistedDraft.CancellationReasons.OwnershipNotActive, notification.CancellationReason);
        Assert.NotNull(notification.CancelledAt);
    }

    [Fact]
    public async Task Event_Loss_And_Device_Fault_Persist_Once_And_Fault_Notifies_Active_Owner()
    {
        using var adminClient = await CreateAuthorizedClientAsync(AuthApiFactory.AdminUserName);
        var deviceId = await RegisterActivateAsync(adminClient, "hw-alarm-fault-01", "عطل");
        var reportUid = Guid.NewGuid();
        var faultUid = Guid.NewGuid();

        var loss1 = await IngestLossAsync(deviceId, reportUid, ownershipVersion: 1, lostCount: 3);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, loss1.Outcome);
        var loss2 = await IngestLossAsync(deviceId, reportUid, ownershipVersion: 1, lostCount: 3);
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, loss2.Outcome);

        var fault1 = await IngestFaultAsync(deviceId, faultUid, ownershipVersion: 1, faultCode: "SENSOR_READ_FAULT");
        Assert.Equal(AlarmEventDraft.IngestOutcomes.Applied, fault1.Outcome);
        Assert.True(fault1.NotificationCreated);
        var fault2 = await IngestFaultAsync(deviceId, faultUid, ownershipVersion: 1, faultCode: "SENSOR_READ_FAULT");
        Assert.Equal(AlarmEventDraft.IngestOutcomes.DuplicateConfirmed, fault2.Outcome);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.EventLossReports.CountAsync(r => r.DeviceId == deviceId && r.ReportUid == reportUid));
        Assert.Equal(1, await db.DeviceFaults.CountAsync(f => f.DeviceId == deviceId && f.FaultUid == faultUid));
        Assert.Equal(1, await db.Notifications.CountAsync(n => n.DeviceFaultId == fault1.DeviceFaultId));
        var loss = await db.EventLossReports.SingleAsync(r => r.ReportUid == reportUid);
        Assert.Equal(DbStatusCodes.EventLossReason.BufferFull, loss.Reason);
        Assert.Equal(3, loss.LostCount);
    }

    private async Task<AlarmEventIngestResultDto> IngestAlarmAsync(
        Guid deviceId,
        Guid eventUid,
        long ownershipVersion,
        bool wasBuffered,
        bool clockTrusted,
        DateTimeOffset? occurredDeviceAt)
        => await IngestRawAsync(
            deviceId,
            deviceId,
            AlarmEventDraft.Topics.Events(deviceId),
            BuildAlarmPayload(deviceId, eventUid, ownershipVersion, wasBuffered, clockTrusted, occurredDeviceAt));

    private async Task<AlarmEventIngestResultDto> IngestLossAsync(
        Guid deviceId,
        Guid reportUid,
        long ownershipVersion,
        int? lostCount)
        => await IngestRawAsync(
            deviceId,
            deviceId,
            AlarmEventDraft.Topics.Events(deviceId),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = AlarmEventDraft.MessageTypes.EventLossReport,
                bootId = "boot-loss",
                reportUid,
                ownershipVersion,
                reason = DbStatusCodes.EventLossReason.BufferFull,
                lostCount,
                clockTrusted = false,
                deviceOccurredAt = (DateTimeOffset?)null
            }));

    private async Task<AlarmEventIngestResultDto> IngestFaultAsync(
        Guid deviceId,
        Guid faultUid,
        long ownershipVersion,
        string faultCode)
        => await IngestRawAsync(
            deviceId,
            deviceId,
            AlarmEventDraft.Topics.Events(deviceId),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                deviceId,
                messageId = Guid.NewGuid(),
                messageType = AlarmEventDraft.MessageTypes.DeviceFault,
                bootId = "boot-fault",
                faultUid,
                ownershipVersion,
                faultCode,
                status = DbStatusCodes.Fault.Active,
                clockTrusted = false,
                deviceOccurredAt = (DateTimeOffset?)null
            }));

    private async Task<AlarmEventIngestResultDto> IngestRawAsync(
        Guid topicDeviceId,
        Guid authenticatedDeviceId,
        string topic,
        string payload)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var ingest = scope.ServiceProvider.GetRequiredService<IAlarmEventIngestService>();
        return await ingest.IngestAsync(new DeviceInboundEnvelope
        {
            TopicDeviceId = topicDeviceId,
            AuthenticatedDeviceId = authenticatedDeviceId,
            Topic = topic,
            PayloadJson = payload
        });
    }

    private static string BuildAlarmPayload(
        Guid deviceId,
        Guid eventUid,
        long ownershipVersion,
        bool wasBuffered,
        bool clockTrusted,
        DateTimeOffset? occurredDeviceAt)
        => JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            deviceId,
            messageId = Guid.NewGuid(),
            messageType = AlarmEventDraft.MessageTypes.AlarmEvent,
            bootId = "boot-alarm",
            eventUid,
            ownershipVersion,
            sensorKey = "vibration-1",
            wasBuffered,
            clockTrusted,
            occurredDeviceAt
        });

    private async Task<Guid> RegisterActivateAsync(HttpClient adminClient, string hardwareUid, string displayName)
    {
        var deviceId = await RegisterDeviceAsync(adminClient, hardwareUid, displayName);
        await LinkAsync(adminClient, deviceId);
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

    private static async Task LinkAsync(HttpClient adminClient, Guid deviceId)
    {
        var link = await adminClient.PostAsJsonAsync($"/api/admin/devices/{deviceId}/link", new
        {
            ownerUserId = AuthApiFactory.OwnerId
        });
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
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
                password = "AlarmSecret!99"
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
