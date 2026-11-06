using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services;
using Application.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

/// <summary>
/// AES-256-GCM encryption for FCM registration tokens. Key from User Secrets / env — not Firebase ADC.
/// </summary>
public sealed class PushTokenCrypto : IPushTokenCrypto
{
    private readonly byte[] _key;

    public PushTokenCrypto(IOptions<FcmOptions> options)
    {
        var raw = options.Value.TokenEncryptionKey;
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Fcm:TokenEncryptionKey is missing. Configure a base64 32-byte key via User Secrets or environment " +
                "(not a Firebase service-account JSON).");
        }

        try
        {
            _key = Convert.FromBase64String(raw.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "Fcm:TokenEncryptionKey must be base64-encoded.", ex);
        }

        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "Fcm:TokenEncryptionKey must decode to exactly 32 bytes (AES-256).");
        }
    }

    public byte[] ComputeFingerprint(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(Normalize(rawToken));
        return SHA256.HashData(bytes);
    }

    public byte[] Encrypt(string rawToken)
    {
        var plaintext = Encoding.UTF8.GetBytes(Normalize(rawToken));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return payload;
    }

    public string Decrypt(byte[] ciphertext)
    {
        if (ciphertext is null || ciphertext.Length < 12 + 16 + 1)
            throw new CryptographicException("Push token ciphertext is truncated.");

        var nonce = ciphertext.AsSpan(0, 12);
        var tag = ciphertext.AsSpan(12, 16);
        var encrypted = ciphertext.AsSpan(28);
        var plaintext = new byte[encrypted.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, encrypted, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static string Normalize(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ArgumentException("Raw push token is empty.", nameof(rawToken));
        return rawToken.Trim();
    }
}
