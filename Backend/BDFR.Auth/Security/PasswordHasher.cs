using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace BDFR.Auth.Security;

public sealed class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public async Task<string> HashAsync(string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = await DeriveAsync(password, salt, cancellationToken);
        return "argon2id$v=19$m=65536,t=3,p=2$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }

    public async Task<bool> VerifyAsync(string password, string encodedHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encodedHash))
            return false;

        var parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id" || parts[1] != "v=19")
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            var actual = await DeriveAsync(password, salt, cancellationToken);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task<byte[]> DeriveAsync(string password, byte[] salt, CancellationToken cancellationToken)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 2,
            Iterations = 3,
            MemorySize = 65536
        };

        cancellationToken.ThrowIfCancellationRequested();
        return await argon2.GetBytesAsync(HashSize);
    }
}
