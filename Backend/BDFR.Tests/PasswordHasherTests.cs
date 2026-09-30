using BDFR.Auth.Security;

namespace BDFR.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public async Task HashAndVerify_RoundTrips()
    {
        var hasher = new PasswordHasher();
        var hash = await hasher.HashAsync("correct-horse-battery-staple");

        Assert.NotEqual("correct-horse-battery-staple", hash);
        Assert.True(await hasher.VerifyAsync("correct-horse-battery-staple", hash));
        Assert.False(await hasher.VerifyAsync("wrong-password", hash));
    }
}
