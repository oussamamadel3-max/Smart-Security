using Application.Options;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests;

/// <summary>
/// Ensures Fake does not require ADC, while Firebase refuses unsafe registration without ADC.
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
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", null);
            // Point ADC away from any ambient gcloud user creds if present by using an invalid path.
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                Path.Combine(Path.GetTempPath(), "smart-security-missing-adc-" + Guid.NewGuid().ToString("N") + ".json"));

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
                    ["Fcm:TokenEncryptionKey"] = Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                })
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.ConfigureInfrastructureServices(config));
            Assert.Contains("Firebase", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fake", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("private_key", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", previous);
        }
    }
}
