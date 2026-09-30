using System.Security.Cryptography;
using System.Text;

namespace BDFR.Security.Cryptography;

public sealed class AesGcmDataProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Encrypt(string plaintext, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var input = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[input.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, input, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, payload, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string payloadBase64, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);
        var payload = Convert.FromBase64String(payloadBase64);

        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid encrypted payload.");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != 32)
            throw new ArgumentException("AES-256 requires a 32-byte key.", nameof(key));
    }
}
