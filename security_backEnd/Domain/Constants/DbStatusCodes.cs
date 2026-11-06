namespace Domain.Constants;

/// <summary>
/// قيم التعدادات المنضبطة المخزَّنة كنص في PostgreSQL (قيود CHECK).
/// </summary>
public static class DbStatusCodes
{
    public static class DeviceLifecycle
    {
        public const string Registered = "REGISTERED";
        public const string Active = "ACTIVE";
        public const string Retired = "RETIRED";
        public const string SqlInList = "'REGISTERED','ACTIVE','RETIRED'";
    }

    public static class Ownership
    {
        public const string Pending = "PENDING";
        public const string Active = "ACTIVE";
        public const string Ended = "ENDED";
        public const string Cancelled = "CANCELLED";
        public const string SqlInList = "'PENDING','ACTIVE','ENDED','CANCELLED'";
    }

    public static class AuthType
    {
        public const string PasswordHash = "PASSWORD_HASH";
        public const string Certificate = "CERTIFICATE";
        public const string SecretRef = "SECRET_REF";
        public const string SqlInList = "'PASSWORD_HASH','CERTIFICATE','SECRET_REF'";
    }

    public static class SensorType
    {
        public const string Vibration = "VIBRATION";
        public const string Motion = "MOTION";
        public const string Other = "OTHER";
        public const string SqlInList = "'VIBRATION','MOTION','OTHER'";
    }

    public static class Diagnostic
    {
        public const string Healthy = "HEALTHY";
        public const string Fault = "FAULT";
        public const string Unknown = "UNKNOWN";
        public const string SqlInList = "'HEALTHY','FAULT','UNKNOWN'";
    }

    public static class Arm
    {
        public const string Armed = "ARMED";
        public const string Disarmed = "DISARMED";
        public const string Unknown = "UNKNOWN";
        public const string SqlInList = "'ARMED','DISARMED','UNKNOWN'";
    }

    public static class Siren
    {
        public const string Off = "OFF";
        public const string Alarm = "ALARM";
        public const string Test = "TEST";
        public const string Unknown = "UNKNOWN";
        public const string SqlInList = "'OFF','ALARM','TEST','UNKNOWN'";
    }

    public static class PowerSource
    {
        public const string Mains = "MAINS";
        public const string Battery = "BATTERY";
        public const string Unknown = "UNKNOWN";
        public const string SqlInList = "'MAINS','BATTERY','UNKNOWN'";
    }

    public static class CommandType
    {
        public const string Arm = "ARM";
        public const string Disarm = "DISARM";
        public const string MuteAlarm = "MUTE_ALARM";
        public const string TestSiren = "TEST_SIREN";
        public const string StopTest = "STOP_TEST";
        public const string SqlInList = "'ARM','DISARM','MUTE_ALARM','TEST_SIREN','STOP_TEST'";
    }

    public static class CommandStatus
    {
        public const string Pending = "PENDING";
        public const string Executed = "EXECUTED";
        public const string Rejected = "REJECTED";
        public const string TimedOut = "TIMED_OUT";
        public const string Cancelled = "CANCELLED";
        public const string SqlInList = "'PENDING','EXECUTED','REJECTED','TIMED_OUT','CANCELLED'";
    }

    public static class AlarmEvent
    {
        public const string Active = "ACTIVE";
        public const string Ended = "ENDED";
        public const string SqlInList = "'ACTIVE','ENDED'";
    }

    public static class Fault
    {
        public const string Active = "ACTIVE";
        public const string Resolved = "RESOLVED";
        public const string SqlInList = "'ACTIVE','RESOLVED'";
    }

    public static class EventLossReason
    {
        public const string BufferFull = "BUFFER_FULL";
    }

    public static class PushPlatform
    {
        public const string Android = "ANDROID";
        public const string Ios = "IOS";
        public const string Web = "WEB";
        public const string SqlInList = "'ANDROID','IOS','WEB'";
    }

    public static class NotificationType
    {
        public const string Alarm = "ALARM";
        public const string Fault = "FAULT";
        public const string AdminAlert = "ADMIN_ALERT";
        public const string SqlInList = "'ALARM','FAULT','ADMIN_ALERT'";
    }

    public static class NotificationStatus
    {
        public const string Queued = "QUEUED";
        public const string Sending = "SENDING";
        public const string Accepted = "ACCEPTED";
        public const string Failed = "FAILED";
        public const string Cancelled = "CANCELLED";
        public const string SqlInList = "'QUEUED','SENDING','ACCEPTED','FAILED','CANCELLED'";
    }

    public static class ProviderResult
    {
        public const string Accepted = "ACCEPTED";
        public const string Retryable = "RETRYABLE";
        public const string Failed = "FAILED";
        public const string SqlInList = "'ACCEPTED','RETRYABLE','FAILED'";
    }

    public static class AuditResult
    {
        public const string Success = "SUCCESS";
        public const string Rejected = "REJECTED";
        public const string Failed = "FAILED";
        public const string SqlInList = "'SUCCESS','REJECTED','FAILED'";
    }

    public static class RoleCodes
    {
        public const string Owner = "OWNER";
        public const string Admin = "ADMIN";
    }
}
