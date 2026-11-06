using Domain.Constants;

namespace Application.Common.Devices;

/// <summary>
/// Pure acceptance gates for owner device commands (BE-T-05 / BE-T-10).
/// HTTP acceptance never implies device execution; only ACK/STATE may change actual arm/siren.
/// </summary>
public static class OwnerDeviceCommandAcceptanceRules
{
    public static bool IsSupportedCommandType(string? commandType)
    {
        if (string.IsNullOrWhiteSpace(commandType))
            return false;

        var normalized = commandType.Trim().ToUpperInvariant();
        return normalized is DbStatusCodes.CommandType.Arm
            or DbStatusCodes.CommandType.Disarm
            or DbStatusCodes.CommandType.MuteAlarm
            or DbStatusCodes.CommandType.TestSiren
            or DbStatusCodes.CommandType.StopTest;
    }

    public static bool IsOwnershipControllable(string ownershipStatus)
        => string.Equals(ownershipStatus, DbStatusCodes.Ownership.Active, StringComparison.Ordinal);

    /// <summary>
    /// PENDING blocks new conflicting commands; TIMED_OUT also blocks until reconciled.
    /// </summary>
    public static bool BlocksConflictingCommand(string status, DateTimeOffset? reconciledAt)
        => status == DbStatusCodes.CommandStatus.Pending
           || (status == DbStatusCodes.CommandStatus.TimedOut && reconciledAt is null);

    public static bool IsAckTimeoutDue(
        DateTimeOffset createdAt,
        DateTimeOffset? sentAt,
        int commandTimeoutSeconds,
        DateTimeOffset utcNow)
    {
        if (commandTimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(commandTimeoutSeconds));

        var anchor = sentAt ?? createdAt;
        return utcNow >= anchor.AddSeconds(commandTimeoutSeconds);
    }

    public static string? EvaluateMuteRule(Guid? targetAlarmEventUid, Guid? currentAlarmEventUid)
    {
        if (targetAlarmEventUid is null || targetAlarmEventUid == Guid.Empty)
            return ExecuteCommandDraft.RejectCodes.MuteTargetRequired;

        if (currentAlarmEventUid is null || currentAlarmEventUid != targetAlarmEventUid)
            return ExecuteCommandDraft.RejectCodes.MuteTargetMismatch;

        return null;
    }

    public static string? EvaluateTestSirenRule(string sirenState, Guid? currentAlarmEventUid)
    {
        if (sirenState == DbStatusCodes.Siren.Alarm || currentAlarmEventUid is not null)
            return ExecuteCommandDraft.RejectCodes.TestDuringAlarm;

        return null;
    }

    public static string? EvaluateStopTestRule(string sirenState)
        => sirenState == DbStatusCodes.Siren.Test
            ? null
            : ExecuteCommandDraft.RejectCodes.StopTestNotActive;

    public static bool MatchesIdempotentReplay(
        Guid existingOwnerUserId,
        Guid requestOwnerUserId,
        Guid existingDeviceId,
        Guid requestDeviceId,
        string existingCommandType,
        string requestCommandType,
        Guid? existingTargetAlarmEventUid,
        Guid? requestTargetAlarmEventUid)
    {
        if (existingOwnerUserId != requestOwnerUserId)
            return false;

        if (existingDeviceId != requestDeviceId)
            return false;

        if (!string.Equals(existingCommandType, requestCommandType, StringComparison.Ordinal))
            return false;

        var expectedTarget = requestCommandType == DbStatusCodes.CommandType.MuteAlarm
            ? requestTargetAlarmEventUid
            : null;

        return existingTargetAlarmEventUid == expectedTarget;
    }

    /// <summary>
    /// HTTP 202 / PENDING / MQTT PUBACK never flip confirmed arm/siren.
    /// </summary>
    public static bool HttpAcceptanceChangesConfirmedArmState() => false;
}
