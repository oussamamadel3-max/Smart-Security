namespace Domain.Constants;

public static class AuditActions
{
    public const string OwnerCreated = "OWNER_CREATED";
    public const string OwnerUpdated = "OWNER_UPDATED";
    public const string OwnerDisabled = "OWNER_DISABLED";
    public const string OwnerArchived = "OWNER_ARCHIVED";
    public const string DeviceRegistered = "DEVICE_REGISTERED";
    public const string DeviceLinked = "DEVICE_LINKED";
    public const string OwnershipActivated = "OWNERSHIP_ACTIVATED";
    public const string DeviceArchived = "DEVICE_ARCHIVED";

    public static class TargetTypes
    {
        public const string User = "USER";
        public const string Device = "DEVICE";
        public const string Ownership = "OWNERSHIP";
    }
}
