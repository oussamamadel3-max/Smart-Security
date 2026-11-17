using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Services;

/// <summary>
/// Validates a Mosquitto server certificate against one configured CA and the connected host name.
/// Revocation is not consulted when the CA has no CRL; chain trust and host name are still required.
/// </summary>
public static class MqttServerCertificateCheck
{
    public static bool IsTrusted(X509Certificate? certificate, string caCertificatePath, string expectedHost)
    {
        if (certificate is null || string.IsNullOrWhiteSpace(caCertificatePath) || string.IsNullOrWhiteSpace(expectedHost))
            return false;
        if (!File.Exists(caCertificatePath))
            return false;

        try
        {
            using var leaf = new X509Certificate2(certificate);
            using var ca = X509Certificate2.CreateFromPem(File.ReadAllText(caCertificatePath));
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            if (!chain.Build(leaf))
                return false;

            var now = DateTime.UtcNow;
            if (leaf.NotBefore.ToUniversalTime() > now || leaf.NotAfter.ToUniversalTime() < now)
                return false;

            return HostIsNamed(leaf, expectedHost.Trim());
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool HostIsNamed(X509Certificate2 leaf, string expectedHost)
    {
        foreach (var extension in leaf.Extensions)
        {
            if (extension.Oid?.Value != "2.5.29.17")
                continue;
            var text = extension.Format(true);
            if (text.Contains(expectedHost, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
