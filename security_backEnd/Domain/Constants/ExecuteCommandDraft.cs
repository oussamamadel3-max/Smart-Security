namespace Domain.Constants;

/// <summary>
/// Draft EXECUTE_COMMAND / outbox markers from development-plan §4.4.
/// Final MQTT schemas remain subject to G1 adoption in <c>integration</c>.
/// </summary>
public static class ExecuteCommandDraft
{
    public const int SchemaVersion = 1;
    public const string MqttMessageType = "EXECUTE_COMMAND";
    public const string OutboxAggregateType = "DEVICE_COMMAND";
    public const string OutboxPublishMessageType = "PUBLISH_DEVICE_COMMAND";

    public static class Topics
    {
        public const string CommandsPattern = "devices/{deviceId}/commands";

        public static string Commands(Guid deviceId) => $"devices/{deviceId:D}/commands";
    }

    public static class RejectCodes
    {
        public const string DeviceOffline = "DEVICE_OFFLINE";
        public const string OwnershipNotControllable = "OWNERSHIP_NOT_CONTROLLABLE";
        public const string CommandConflict = "COMMAND_CONFLICT";
        public const string AwaitingReconciliation = "AWAITING_RECONCILIATION";
        public const string MuteTargetRequired = "MUTE_TARGET_REQUIRED";
        public const string MuteTargetMismatch = "MUTE_TARGET_MISMATCH";
        public const string TestDuringAlarm = "TEST_DURING_ALARM";
        public const string StopTestNotActive = "STOP_TEST_NOT_ACTIVE";
        public const string UnsupportedCommandType = "UNSUPPORTED_COMMAND_TYPE";
        public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
        public const string CommandExpired = "COMMAND_EXPIRED";
        public const string OwnershipVersionStale = "OWNERSHIP_VERSION_STALE";
        public const string AggregateMissing = "AGGREGATE_MISSING";
        public const string PublishDisabled = "MQTT_OUTBOX_DISABLED";
        public const string PublishFailed = "MQTT_PUBLISH_FAILED";
    }

    public static class HistoryReasons
    {
        public const string Accepted = "ACCEPTED";
        public const string Published = "PUBLISHED";
        public const string TimedOut = "ACK_TIMEOUT";
        public const string LateAck = "LATE_ACK";
        public const string ReconciledByState = "RECONCILED_BY_STATE";
        public const string DeviceAck = "DEVICE_ACK";
    }
}
