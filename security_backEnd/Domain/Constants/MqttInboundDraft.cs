namespace Domain.Constants;

/// <summary>
/// Draft inbound MQTT shapes from development-plan §4.1 / §4.4.
/// Final schemas remain subject to G1 adoption in <c>integration</c>.
/// </summary>
public static class MqttInboundDraft
{
    public const int SchemaVersion = 1;

    public static class MessageTypes
    {
        public const string State = "STATE";
        public const string Heartbeat = "HEARTBEAT";
        public const string CommandAck = "COMMAND_ACK";
        public const string AlarmEvent = AlarmEventDraft.MessageTypes.AlarmEvent;
        public const string EventLossReport = AlarmEventDraft.MessageTypes.EventLossReport;
        public const string DeviceFault = AlarmEventDraft.MessageTypes.DeviceFault;
    }

    public static class Topics
    {
        public const string StatePattern = "devices/{deviceId}/state";
        public const string AcksPattern = "devices/{deviceId}/acks";
        public const string EventsPattern = AlarmEventDraft.Topics.EventsPattern;

        public static string State(Guid deviceId) => $"devices/{deviceId:D}/state";

        public static string Acks(Guid deviceId) => $"devices/{deviceId:D}/acks";

        public static string Events(Guid deviceId) => AlarmEventDraft.Topics.Events(deviceId);
    }

    public static class ConnectionStatus
    {
        public const string Online = "ONLINE";
        public const string Offline = "OFFLINE";
    }

    public static class IngestOutcomes
    {
        public const string Applied = "APPLIED";
        public const string IgnoredStaleSequence = "IGNORED_STALE_SEQUENCE";
        public const string HeartbeatOnly = "HEARTBEAT_ONLY";
        public const string Rejected = "REJECTED";
        /// <summary>
        /// COMMAND_ACK closed an unresolved command without moving device_states (stale/equal sequence).
        /// </summary>
        public const string CommandClosedOnly = "COMMAND_CLOSED_ONLY";
    }
}
