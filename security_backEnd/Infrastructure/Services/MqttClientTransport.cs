using MQTTnet;

namespace Infrastructure.Services;

internal static class MqttClientTransport
{
    public static void ApplyTls(MqttClientOptionsBuilder builder, bool useTls, string? caCertificatePath, string host)
    {
        if (!useTls)
            return;

        if (string.IsNullOrWhiteSpace(caCertificatePath))
            throw new InvalidOperationException("MQTT TLS is enabled but Mqtt CaCertificatePath is empty.");

        var caPath = caCertificatePath;
        var expectedHost = host;
        builder.WithTlsOptions(options =>
        {
            options.UseTls();
            options.WithCertificateValidationHandler(args =>
                MqttServerCertificateCheck.IsTrusted(args.Certificate, caPath, expectedHost));
        });
    }
}
