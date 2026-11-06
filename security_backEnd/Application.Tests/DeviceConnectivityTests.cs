using Application.Common.Devices;
using Domain.Constants;

namespace Application.Tests;

public sealed class DeviceConnectivityTests
{
    [Fact]
    public void Null_LastSeen_Is_Offline()
    {
        var now = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        var status = DeviceConnectivity.ComputeStatus(null, 3600, now);
        Assert.Equal(MqttInboundDraft.ConnectionStatus.Offline, status);
    }

    [Fact]
    public void Recent_LastSeen_Is_Online_Independent_Of_Arm()
    {
        var now = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        var lastSeen = now.AddMinutes(-10);
        Assert.Equal(
            MqttInboundDraft.ConnectionStatus.Online,
            DeviceConnectivity.ComputeStatus(lastSeen, 3600, now));
    }

    [Fact]
    public void Stale_LastSeen_Is_Offline_After_Heartbeat_Timeout()
    {
        var now = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        var lastSeen = now.AddMinutes(-61);
        Assert.Equal(
            MqttInboundDraft.ConnectionStatus.Offline,
            DeviceConnectivity.ComputeStatus(lastSeen, 3600, now));
    }

    [Fact]
    public void Boundary_Exactly_At_Timeout_Remains_Online()
    {
        var now = DateTimeOffset.Parse("2026-11-06T12:00:00Z");
        var lastSeen = now.AddSeconds(-3600);
        Assert.Equal(
            MqttInboundDraft.ConnectionStatus.Online,
            DeviceConnectivity.ComputeStatus(lastSeen, 3600, now));
    }
}
