namespace Domain.Constants;

/// <summary>
/// Draft OWNERSHIP_SYNC field names and outbox markers from the development-plan §4.4 example.
/// Not a G1-adopted production contract; publish worker and final schema remain gated.
/// </summary>
public static class OwnershipSyncDraft
{
    public const int SchemaVersion = 1;
    public const string MqttMessageType = "OWNERSHIP_SYNC";
    public const string OutboxAggregateType = "DEVICE_OWNERSHIP";
    public const string OutboxPublishMessageType = "PUBLISH_OWNERSHIP_SYNC";
}
