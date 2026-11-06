namespace Domain.Constants;

/// <summary>
/// Auth session / refresh channel constants aligned with CHECK constraints.
/// </summary>
public static class AuthStatusCodes
{
    public static class RefreshChannel
    {
        public const string Cookie = "COOKIE";
        public const string Body = "BODY";
        public const string SqlInList = "'COOKIE','BODY'";
    }

    public static class RevokeReason
    {
        public const string Logout = "LOGOUT";
        public const string LogoutAll = "LOGOUT_ALL";
        public const string RefreshReuse = "REFRESH_REUSE";
        public const string UserDisabled = "USER_DISABLED";
        public const string UserArchived = "USER_ARCHIVED";
        public const string AbsoluteExpired = "ABSOLUTE_EXPIRED";
        public const string Replaced = "REPLACED";
    }

    public static class TokenUse
    {
        public const string Access = "access";
    }
}
