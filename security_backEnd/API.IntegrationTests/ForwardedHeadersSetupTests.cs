using API.Configuration;
using Microsoft.Extensions.Configuration;

namespace API.IntegrationTests;

/// <summary>
/// OPS-T-03: Forwarded Headers must not trust arbitrary clients by default.
/// </summary>
public sealed class ForwardedHeadersSetupTests
{
    [Fact]
    public void Default_Does_Not_Enable_Explicit_Allow_List()
    {
        var snap = ForwardedHeadersSetup.Evaluate(new ConfigurationBuilder().Build());

        Assert.False(snap.UsedExplicitAllowList);
        Assert.Equal(1, snap.ForwardLimit);
    }

    [Fact]
    public void TrustPrivateNetworkProxies_Uses_Explicit_Allow_List_With_Networks()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:TrustPrivateNetworkProxies"] = "true"
            })
            .Build();

        var snap = ForwardedHeadersSetup.Evaluate(config);

        Assert.True(snap.UsedExplicitAllowList);
        Assert.True(snap.KnownNetworkCount >= 3);
    }

    [Fact]
    public void Explicit_KnownProxy_Is_Counted()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "10.10.0.5"
            })
            .Build();

        var snap = ForwardedHeadersSetup.Evaluate(config);

        Assert.True(snap.UsedExplicitAllowList);
        Assert.Equal(1, snap.KnownProxyCount);
    }
}
