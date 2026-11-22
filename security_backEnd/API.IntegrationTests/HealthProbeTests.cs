using System.Net;
using API.IntegrationTests.Support;

namespace API.IntegrationTests;

/// <summary>
/// OPS-T-03: liveness vs readiness probes must be distinguishable.
/// </summary>
[Collection(nameof(AuthHttpCollection))]
public sealed class HealthProbeTests
{
    [SkippableFact]
    public async Task Live_Probe_Returns_Ok_Without_Requiring_Business_Routes()
    {
        AuthApiFactory? factory = null;
        try
        {
            factory = new AuthApiFactory();
        }
        catch (InvalidOperationException ex)
        {
            Skip.If(true, ex.Message);
        }

        Assert.NotNull(factory);
        await using var host = factory;
        using var client = host.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task Ready_Probe_Returns_Ok_When_Database_Is_Reachable()
    {
        AuthApiFactory? factory = null;
        try
        {
            factory = new AuthApiFactory();
        }
        catch (InvalidOperationException ex)
        {
            Skip.If(true, ex.Message);
        }

        Assert.NotNull(factory);
        await using var host = factory;
        using var client = host.CreateClient();
        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task Legacy_Health_Alias_Remains_Liveness()
    {
        AuthApiFactory? factory = null;
        try
        {
            factory = new AuthApiFactory();
        }
        catch (InvalidOperationException ex)
        {
            Skip.If(true, ex.Message);
        }

        Assert.NotNull(factory);
        await using var host = factory;
        using var client = host.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
