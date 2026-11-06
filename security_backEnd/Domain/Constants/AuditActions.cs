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
    public const string OwnershipTransferred = "OWNERSHIP_TRANSFERRED";
    public const string OwnershipUnlinked = "OWNERSHIP_UNLINKED";
    public const string DeviceArchived = "DEVICE_ARCHIVED";
    public const string DeviceCommandAccepted = "DEVICE_COMMAND_ACCEPTED";
    public const string AlarmEventPersisted = "ALARM_EVENT_PERSISTED";
    public const string EventLossReportPersisted = "EVENT_LOSS_REPORT_PERSISTED";
    public const string DeviceFaultPersisted = "DEVICE_FAULT_PERSISTED";

    public static class TargetTypes
    {
        public const string User = "USER";
        public const string Device = "DEVICE";
        public const string Ownership = "OWNERSHIP";
        public const string Command = "COMMAND";
        public const string AlarmEvent = "ALARM_EVENT";
        public const string EventLossReport = "EVENT_LOSS_REPORT";
        public const string DeviceFault = "DEVICE_FAULT";
    }
}

