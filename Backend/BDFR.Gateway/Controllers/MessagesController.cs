using System.Security.Claims;
using BDFR.Database;
using BDFR.Database.Models;
using BDFR.Gateway.Realtime;
using BDFR.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Controllers;

public sealed record SendMessageRequest(string Content);

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController(
    MessengerDbContext db,
    IHubContext<ChatHub> hub,
    AesGcmDataProtector protector,
    IConfiguration configuration) : ControllerBase
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
            ContentEncrypted = protector.Encrypt(content, GetDataKey())
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        var dto = new
        {
            message.Id,
            message.SenderId,
            message.RecipientId,
            Content = content,
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

        var rows = await db.Messages
            .Where(x =>
                (x.SenderId == me && x.RecipientId == otherUserId) ||
                (x.SenderId == otherUserId && x.RecipientId == me))
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var key = GetDataKey();
        var messages = rows.Select(x => new
        {
            x.Id,
            x.SenderId,
            x.RecipientId,
            Content = protector.Decrypt(x.ContentEncrypted, key),
            x.CreatedAt,
            x.DeliveredAt,
            x.ReadAt
        });

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

    private byte[] GetDataKey()
    {
        var encoded = configuration["Security:DataEncryptionKeyBase64"]
            ?? throw new InvalidOperationException("Data encryption key is missing.");
        var key = Convert.FromBase64String(encoded);
        if (key.Length != 32) throw new InvalidOperationException("Data encryption key must be 32 bytes.");
        return key;
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
