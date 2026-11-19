using Application.Options;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

/// <summary>
/// Ensures Fake does not require ADC, while Firebase refuses unsafe registration without ADC
/// and rejects cross-environment credential project_id mismatch.
/// </summary>
public sealed class FcmProviderRegistrationTests
{
    [Fact]
    public void Fake_Provider_Registers_Without_Google_Application_Credentials()
    {
        var previous = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);

            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=127.0.0.1;Port=5432;Database=fcm_probe_unused;Username=postgres",
                    ["Auth:Jwt:SigningKey"] = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                    ["Auth:Jwt:Issuer"] = "probe",
                    ["Auth:Jwt:Audience"] = "probe",
                    ["Fcm:Provider"] = FcmOptions.ProviderFake,
                    ["Fcm:Enabled"] = "false",
                    ["Fcm:TokenEncryptionKey"] = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                })
                .Build();

            // Registration of Fake must not throw due to missing ADC.
            services.ConfigureInfrastructureServices(config);
            var provider = services.BuildServiceProvider();
            var fcm = provider.GetRequiredService<Application.Interfaces.Services.IFcmProvider>();
            Assert.Equal(FcmOptions.ProviderFake, fcm.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", previous);
        }
    }

    [Fact]
    public void Firebase_Provider_Without_Adc_Throws_At_Registration()
    {
        var previous = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        try
        {
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                Path.Combine(Path.GetTempPath(), "smart-security-missing-adc-" + Guid.NewGuid().ToString("N") + ".json"));

            var services = new ServiceCollection();
            var config = BuildFirebaseConfig(projectId: "smart-security-dev-28642");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureInfrastructureServices(config));
            Assert.Contains("Firebase", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fake", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.True(
                ex.Message.Contains("GOOGLE_APPLICATION_CREDENTIALS", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Application Default Credentials", StringComparison.OrdinalIgnoreCase),
                "Expected an ADC / credentials-path failure message.");
            Assert.DoesNotContain("private_key", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", previous);
        }
    }

    [Fact]
    public void Firebase_Provider_Without_FirebaseProjectId_Throws_At_Registration()
    {
        var previous = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);

            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=127.0.0.1;Port=5432;Database=fcm_probe_unused;Username=postgres",
                    ["Auth:Jwt:SigningKey"] = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                    ["Auth:Jwt:Issuer"] = "probe",
                    ["Auth:Jwt:Audience"] = "probe",
                    ["Fcm:Provider"] = FcmOptions.ProviderFirebase,
                    ["Fcm:Enabled"] = "false",
                    ["Fcm:FirebaseProjectId"] = "",
                    ["Fcm:TokenEncryptionKey"] = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                })
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureInfrastructureServices(config));
            Assert.Contains("FirebaseProjectId", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private_key", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", previous);
        }
    }

    [Fact]
    public void Firebase_Provider_With_Mismatched_Credential_ProjectId_Throws()
    {
        var previous = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        var tempPath = Path.Combine(Path.GetTempPath(), "fcm-c02-mismatch-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            // Minimal JSON — only project_id is read for the mismatch guard (no private_key logged).
            File.WriteAllText(
                tempPath,
                """{"type":"service_account","project_id":"smart-security-stage"}""");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);

            var services = new ServiceCollection();
            var config = BuildFirebaseConfig(projectId: "smart-security-dev-28642");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureInfrastructureServices(config));
            Assert.Contains("project_id", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FirebaseProjectId", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private_key", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("smart-security-stage", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", previous);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static IConfiguration BuildFirebaseConfig(string projectId) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=5432;Database=fcm_probe_unused;Username=postgres",
                ["Auth:Jwt:SigningKey"] = Convert.ToBase64String(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
                ["Auth:Jwt:Issuer"] = "probe",
                ["Auth:Jwt:Audience"] = "probe",
                ["Fcm:Provider"] = FcmOptions.ProviderFirebase,
                ["Fcm:Enabled"] = "false",
                ["Fcm:FirebaseProjectId"] = projectId,
                ["Fcm:TokenEncryptionKey"] = Convert.ToBase64String(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            })
            .Build();
}
