using Application.Options;
using Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Application.Tests;

public sealed class PushTokenCryptoTests
{
    private static PushTokenCrypto CreateCrypto()
    {
        var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        return new PushTokenCrypto(Microsoft.Extensions.Options.Options.Create(new FcmOptions { TokenEncryptionKey = key }));
    }

    [Fact]
    public void RoundTrip_Encrypt_Decrypt_Preserves_Token()
    {
        var crypto = CreateCrypto();
        const string raw = "fcm-registration-token-example-001";
        var cipher = crypto.Encrypt(raw);
        Assert.NotEmpty(cipher);
        Assert.Equal(raw, crypto.Decrypt(cipher));
    }

    [Fact]
    public void Fingerprint_Is_Deterministic_And_Not_Raw_Token()
    {
        var crypto = CreateCrypto();
        const string raw = "fcm-registration-token-example-002";
        var a = crypto.ComputeFingerprint(raw);
        var b = crypto.ComputeFingerprint(raw);
        Assert.Equal(a, b);
        Assert.Equal(32, a.Length);
        Assert.False(Convert.ToHexString(a).Contains(raw, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Missing_Key_Throws_Without_Embedding_Secrets()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PushTokenCrypto(Microsoft.Extensions.Options.Options.Create(new FcmOptions { TokenEncryptionKey = "" })));
        Assert.Contains("TokenEncryptionKey", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private_key", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Corrupt_Base64_Key_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PushTokenCrypto(Microsoft.Extensions.Options.Options.Create(
                new FcmOptions { TokenEncryptionKey = "not-valid-base64!!!" })));
        Assert.Contains("base64", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wrong_Length_Key_Throws()
    {
        var shortKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PushTokenCrypto(Microsoft.Extensions.Options.Options.Create(
                new FcmOptions { TokenEncryptionKey = shortKey })));
        Assert.Contains("32 bytes", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Different_Keys_Cannot_Decrypt()
    {
        var a = CreateCrypto();
        var b = CreateCrypto();
        var cipher = a.Encrypt("fcm-registration-token-example-003");
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => b.Decrypt(cipher));
    }

    [Fact]
    public void Ciphertext_Is_Not_Plaintext_Utf8()
    {
        var crypto = CreateCrypto();
        const string raw = "fcm-registration-token-example-004";
        var cipher = crypto.Encrypt(raw);
        var asText = System.Text.Encoding.UTF8.GetString(cipher);
        Assert.DoesNotContain(raw, asText, StringComparison.Ordinal);
    }
}
