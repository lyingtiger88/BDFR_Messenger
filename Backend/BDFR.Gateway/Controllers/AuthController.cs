using System.Security.Cryptography;
using System.Text;
using BDFR.Auth.Security;
using BDFR.Auth.Tokens;
using BDFR.Database;
using BDFR.Database.Models;
using BDFR.Gateway.Contracts;
using BDFR.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    MessengerDbContext db,
    PasswordHasher passwords,
    TokenService tokens,
    AesGcmDataProtector protector,
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);

        if (username.Length is < 3 or > 64)
            return BadRequest(new { error = "Username must be 3-64 characters." });
        if (request.Password.Length < 10)
            return BadRequest(new { error = "Password must contain at least 10 characters." });
        if (!email.Contains('@'))
            return BadRequest(new { error = "A valid email address is required." });

        var emailHash = LookupHash(email);
        if (await db.Users.AnyAsync(x => x.Username == username || x.EmailLookupHash == emailHash, ct))
            return Conflict(new { error = "Username or email is already registered." });

        var key = GetDataKey();
        var user = new User
        {
            Username = username,
            EmailEncrypted = protector.Encrypt(email, key),
            EmailLookupHash = emailHash,
            PasswordHash = await passwords.HashAsync(request.Password, ct)
        };

        db.Users.Add(user);
        var response = IssueSession(user, request.DeviceName, request.Platform);
        await db.SaveChangesAsync(ct);

        return Created($"/api/users/{user.Id}", response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var emailHash = login.Contains('@') ? LookupHash(login) : null;

        var user = await db.Users.FirstOrDefaultAsync(
            x => x.IsActive && (x.Username == login || (emailHash != null && x.EmailLookupHash == emailHash)), ct);

        if (user is null || !await passwords.VerifyAsync(request.Password, user.PasswordHash, ct))
            return Unauthorized(new { error = "Invalid credentials." });

        user.LastSeenAt = DateTimeOffset.UtcNow;
        var response = IssueSession(user, request.DeviceName, request.Platform);
        await db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var hash = TokenService.HashRefreshToken(request.RefreshToken);
        var session = await db.Sessions.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.RefreshTokenHash == hash, ct);

        if (session is null || session.RevokedAt != null || session.ExpiresAt <= DateTimeOffset.UtcNow || !session.User.IsActive)
            return Unauthorized(new { error = "Refresh token is invalid or expired." });

        session.RevokedAt = DateTimeOffset.UtcNow;
        var response = IssueSession(session.User, session.DeviceName, session.Platform);
        await db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        var hash = TokenService.HashRefreshToken(request.RefreshToken);
        var session = await db.Sessions.FirstOrDefaultAsync(x => x.RefreshTokenHash == hash, ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    private AuthResponse IssueSession(User user, string? deviceName, string? platform)
    {
        var refresh = TokenService.CreateRefreshToken();
        db.Sessions.Add(new Session
        {
            User = user,
            UserId = user.Id,
            RefreshTokenHash = TokenService.HashRefreshToken(refresh),
            DeviceName = deviceName?.Trim(),
            Platform = platform?.Trim(),
            ExpiresAt = DateTimeOffset.UtcNow.Add(RefreshLifetime)
        });

        var accessExpiry = DateTimeOffset.UtcNow.Add(AccessLifetime);
        var access = tokens.CreateAccessToken(user.Id, user.Username, AccessLifetime);
        return new AuthResponse(user.Id, user.Username, access, refresh, accessExpiry);
    }

    private byte[] GetDataKey()
    {
        var encoded = configuration["Security:DataEncryptionKeyBase64"]
            ?? throw new InvalidOperationException("Data encryption key is missing.");
        var key = Convert.FromBase64String(encoded);
        if (key.Length != 32) throw new InvalidOperationException("Data encryption key must be 32 bytes.");
        return key;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();
    private static string LookupHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
