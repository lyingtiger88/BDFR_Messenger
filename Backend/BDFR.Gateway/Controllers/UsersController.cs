using BDFR.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(MessengerDbContext db) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        q = (q ?? string.Empty).Trim().ToLowerInvariant();
        if (q.Length < 2) return Ok(Array.Empty<object>());

        var users = await db.Users
            .Where(x => x.IsActive && x.Username.Contains(q))
            .OrderBy(x => x.Username)
            .Take(20)
            .Select(x => new { x.Id, x.Username, x.LastSeenAt })
            .ToListAsync(ct);

        return Ok(users);
    }
}
