using System.Security.Claims;
using BDFR.Database;
using BDFR.Gateway.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(MessengerDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.CreatedAt,
                x.LastSeenAt,
                x.IsActive,
                x.IsSellerVerified,
                HasStore = db.MarketplaceStores.Any(s => s.OwnerUserId == x.Id)
            })
            .FirstOrDefaultAsync(ct);

        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(
            user.Id,
            user.Username,
            user.CreatedAt,
            user.LastSeenAt,
            user.IsActive,
            user.IsSellerVerified,
            user.IsSellerVerified ? "verified_seller" : null,
            user.IsSellerVerified ? "blue" : null,
            user.HasStore));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        q = (q ?? string.Empty).Trim().ToLowerInvariant();
        if (q.Length < 2) return Ok(Array.Empty<object>());

        var users = await db.Users
            .Where(x => x.IsActive && x.Username.Contains(q))
            .OrderBy(x => x.Username)
            .Take(20)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.LastSeenAt,
                x.IsSellerVerified,
                VerificationBadge = x.IsSellerVerified ? "verified_seller" : null,
                VerificationBadgeColor = x.IsSellerVerified ? "blue" : null,
                HasStore = db.MarketplaceStores.Any(s => s.OwnerUserId == x.Id)
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
