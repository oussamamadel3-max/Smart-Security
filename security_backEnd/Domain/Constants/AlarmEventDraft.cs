namespace Domain.Constants;

/// <summary>
/// Draft inbound ALARM_EVENT / EVENT_LOSS_REPORT / DEVICE_FAULT shapes from development-plan §4.4.
/// Final schemas remain subject to G1 adoption in <c>integration</c>.
/// </summary>
public static class AlarmEventDraft
{
    public const int SchemaVersion = 1;

    public static class MessageTypes
    {
        public const string AlarmEvent = "ALARM_EVENT";
        public const string EventLossReport = "EVENT_LOSS_REPORT";
        public const string DeviceFault = "DEVICE_FAULT";
    }

    /// <summary>
    /// Lifecycle of one <c>eventUid</c>. Missing on the wire means <see cref="Active"/>.
    /// <see cref="Ended"/> is the sensor returning to rest, not <c>MUTE_ALARM</c>.
    /// </summary>
    public static class AlarmPhase
    {
        public const string Active = "ACTIVE";
        public const string Ended = "ENDED";
    }

    public static class Topics
    {
        public const string EventsPattern = "devices/{deviceId}/events";

        public static string Events(Guid deviceId) => $"devices/{deviceId:D}/events";
    }

    public static class IngestOutcomes
    {
        public const string Applied = "APPLIED";
        public const string DuplicateConfirmed = "DUPLICATE_CONFIRMED";
        public const string Rejected = "REJECTED";
    }
}
