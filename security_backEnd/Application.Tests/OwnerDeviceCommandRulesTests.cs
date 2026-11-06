using Application.Common.Devices;
using Domain.Constants;

namespace Application.Tests;

/// <summary>
/// Pure rule checks used by command acceptance (connectivity + type gates documented in SRS).
/// </summary>
public sealed class OwnerDeviceCommandRulesTests
{
    [Fact]
    public void Connectivity_Gate_Uses_Heartbeat_Timeout_Not_Stored_Flag()
    {
        var now = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        Assert.True(DeviceConnectivity.IsOnline(now.AddMinutes(-10), 3600, now));
        Assert.False(DeviceConnectivity.IsOnline(now.AddMinutes(-61), 3600, now));
        Assert.False(DeviceConnectivity.IsOnline(null, 3600, now));
    }

    [Theory]
    [InlineData(DbStatusCodes.CommandType.Arm)]
    [InlineData(DbStatusCodes.CommandType.Disarm)]
    [InlineData(DbStatusCodes.CommandType.MuteAlarm)]
    [InlineData(DbStatusCodes.CommandType.TestSiren)]
    [InlineData(DbStatusCodes.CommandType.StopTest)]
    public void Supported_Command_Types_Match_Db_Check_Constraint(string type)
    {
        Assert.Contains($"'{type}'", DbStatusCodes.CommandType.SqlInList, StringComparison.Ordinal);
    }

    [Fact]
    public void ExecuteCommand_Draft_Markers_Are_Stable_Until_G1()
    {
        Assert.Equal("EXECUTE_COMMAND", ExecuteCommandDraft.MqttMessageType);
        Assert.Equal("PUBLISH_DEVICE_COMMAND", ExecuteCommandDraft.OutboxPublishMessageType);
        Assert.Equal("DEVICE_COMMAND", ExecuteCommandDraft.OutboxAggregateType);
    }
}
