namespace Domain.Constants;

/// <summary>
/// Draft EVENT_PERSISTED / outbox markers from development-plan §4.4.
/// Final MQTT schemas remain subject to G1 adoption in <c>integration</c>.
/// </summary>
public static class EventPersistedDraft
{
    public const int SchemaVersion = 1;
    public const string MqttMessageType = "EVENT_PERSISTED";
    public const string OutboxAggregateType = "ALARM_EVENT";
    public const string OutboxPublishMessageType = "PUBLISH_EVENT_PERSISTED";

    public static class Topics
    {
        /// <summary>
        /// Server→device channel shared with EXECUTE_COMMAND / OWNERSHIP_SYNC (draft §4.1).
        /// </summary>
        public static string Commands(Guid deviceId) => $"devices/{deviceId:D}/commands";
    }

    public static class RejectCodes
    {
        public const string AggregateMissing = "AGGREGATE_MISSING";
        public const string PublishDisabled = "MQTT_OUTBOX_DISABLED";
        public const string PublishFailed = "MQTT_PUBLISH_FAILED";
    }

    public static class CancellationReasons
    {
        /// <summary>
        /// Recipient no longer holds ACTIVE ownership for the alarm's device/ownership.
        /// Used by <c>INotificationRecipientGuard</c> before FCM (T-09).
        /// </summary>
        public const string OwnershipNotActive = "OWNERSHIP_NOT_ACTIVE";

        /// <summary>
        /// Bulk cancel inside the ownership transfer/unlink transaction (T-07).
        /// Does not cancel independent ADMIN_ALERT notification rows.
        /// </summary>
        public const string OwnershipTransferred = "OWNERSHIP_TRANSFERRED";
    }
}
