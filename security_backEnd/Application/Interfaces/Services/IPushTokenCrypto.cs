namespace Application.Interfaces.Services;

/// <summary>
/// Encrypts FCM registration tokens at rest and computes deterministic fingerprints.
/// </summary>
public interface IPushTokenCrypto
{
    byte[] ComputeFingerprint(string rawToken);

    byte[] Encrypt(string rawToken);

    string Decrypt(byte[] ciphertext);
}
