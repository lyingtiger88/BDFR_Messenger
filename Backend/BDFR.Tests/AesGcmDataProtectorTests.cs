using System.Security.Cryptography;
using BDFR.Security.Cryptography;

namespace BDFR.Tests;

public sealed class AesGcmDataProtectorTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrips()
    {
        var protector = new AesGcmDataProtector();
        var key = RandomNumberGenerator.GetBytes(32);

        var encrypted = protector.Encrypt("user@example.com", key);
        var decrypted = protector.Decrypt(encrypted, key);

        Assert.NotEqual("user@example.com", encrypted);
        Assert.Equal("user@example.com", decrypted);
    }
}
