using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace API.Configuration;

/// <summary>
/// Result of applying Forwarded Headers trust policy (for ops/tests).
/// </summary>
public sealed record ForwardedHeadersTrustSnapshot(
    int KnownProxyCount,
    int KnownNetworkCount,
    int ForwardLimit,
    bool UsedExplicitAllowList);

/// <summary>
/// OPS-T-03: configure Forwarded Headers so only explicitly trusted proxies/networks
/// can influence scheme/client IP. Clearing Known* without an allow-list would trust every client.
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string SectionName = "ForwardedHeaders";

    public static ForwardedHeadersTrustSnapshot Configure(
        ForwardedHeadersOptions options,
        IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.RequireHeaderSymmetry = false;

        var section = configuration.GetSection(SectionName);
        var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];
        var trustPrivate = section.GetValue("TrustPrivateNetworkProxies", false);

        if (proxies.Length == 0 && networks.Length == 0 && !trustPrivate)
        {
            // Keep ASP.NET defaults (loopback only). Do not clear KnownProxies/KnownIPNetworks.
            return new ForwardedHeadersTrustSnapshot(
                options.KnownProxies.Count,
                options.KnownIPNetworks.Count,
                options.ForwardLimit ?? 1,
                UsedExplicitAllowList: false);
        }

        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var proxy in proxies)
        {
            if (IPAddress.TryParse(proxy.Trim(), out var ip))
                options.KnownProxies.Add(ip);
        }

        foreach (var cidr in networks)
            TryAddNetwork(options, cidr);

        if (trustPrivate)
        {
            // Intended only when Kestrel is not reachable by untrusted clients (e.g. Docker-internal only).
            TryAddNetwork(options, "10.0.0.0/8");
            TryAddNetwork(options, "172.16.0.0/12");
            TryAddNetwork(options, "192.168.0.0/16");
            TryAddNetwork(options, "fc00::/7");
        }

        return new ForwardedHeadersTrustSnapshot(
            options.KnownProxies.Count,
            options.KnownIPNetworks.Count,
            options.ForwardLimit ?? 1,
            UsedExplicitAllowList: true);
    }

    /// <summary>
    /// Applies configuration to a fresh options instance (unit-test friendly without HttpOverrides types in callers).
    /// </summary>
    public static ForwardedHeadersTrustSnapshot Evaluate(IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions();
        return Configure(options, configuration);
    }

    internal static bool TryAddNetwork(ForwardedHeadersOptions options, string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        try
        {
            var network = System.Net.IPNetwork.Parse(cidr.Trim());
            options.KnownIPNetworks.Add(network);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
