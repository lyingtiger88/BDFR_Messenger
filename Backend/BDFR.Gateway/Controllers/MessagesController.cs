using System.Security.Claims;
using BDFR.Database;
using BDFR.Database.Models;
using BDFR.Gateway.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

public sealed record SendMessageRequest(string Content);

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController(MessengerDbContext db, IHubContext<ChatHub> hub) : ControllerBase
{
    [HttpPost("to/{recipientId:guid}")]
    public async Task<IActionResult> Send(Guid recipientId, SendMessageRequest request, CancellationToken ct)
    {
        var senderId = CurrentUserId();
        var content = (request.Content ?? string.Empty).Trim();

        if (content.Length is < 1 or > 4000)
            return BadRequest(new { error = "Message must contain 1-4000 characters." });

        if (!await db.Users.AnyAsync(x => x.Id == recipientId && x.IsActive, ct))
            return NotFound(new { error = "Recipient does not exist." });

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        var dto = new
        {
            message.Id,
            message.SenderId,
            message.RecipientId,
            message.Content,
            message.CreatedAt
        };

        await hub.Clients.User(recipientId.ToString()).SendAsync("messageReceived", dto, ct);
        return Created($"/api/messages/{message.Id}", dto);
    }

    [HttpGet("with/{otherUserId:guid}")]
    public async Task<IActionResult> GetConversation(Guid otherUserId, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var me = CurrentUserId();
        take = Math.Clamp(take, 1, 100);

        var messages = await db.Messages
            .Where(x =>
                (x.SenderId == me && x.RecipientId == otherUserId) ||
                (x.SenderId == otherUserId && x.RecipientId == me))
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.SenderId,
                x.RecipientId,
                x.Content,
                x.CreatedAt,
                x.DeliveredAt,
                x.ReadAt
            })
            .ToListAsync(ct);

        return Ok(messages);
    }

    [HttpPost("{messageId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid messageId, CancellationToken ct)
    {
        var me = CurrentUserId();
        var message = await db.Messages.FirstOrDefaultAsync(x => x.Id == messageId && x.RecipientId == me, ct);
        if (message is null) return NotFound();

        message.DeliveredAt ??= DateTimeOffset.UtcNow;
        message.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
