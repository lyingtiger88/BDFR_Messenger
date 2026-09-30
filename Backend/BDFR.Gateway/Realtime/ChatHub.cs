using System.Security.Claims;
using BDFR.Database;
using BDFR.Database.Models;
using BDFR.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Realtime;

[Authorize]
public sealed class ChatHub(
    MessengerDbContext db,
    AesGcmDataProtector protector,
    IConfiguration configuration) : Hub
{
    public async Task SendDirectMessage(Guid recipientId, string content)
    {
        var senderId = CurrentUserId();
        content = content?.Trim() ?? string.Empty;

        if (content.Length is < 1 or > 4000)
            throw new HubException("Message must contain 1-4000 characters.");

        if (!await db.Users.AnyAsync(x => x.Id == recipientId && x.IsActive))
            throw new HubException("Recipient does not exist.");

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            ContentEncrypted = protector.Encrypt(content, GetDataKey())
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var dto = new
        {
            message.Id,
            message.SenderId,
            message.RecipientId,
            Content = content,
            message.CreatedAt
        };

        await Clients.User(recipientId.ToString()).SendAsync("messageReceived", dto);
        await Clients.Caller.SendAsync("messageSent", dto);
    }

    public async Task Typing(Guid recipientId, bool isTyping)
    {
        await Clients.User(recipientId.ToString())
            .SendAsync("typing", new { userId = CurrentUserId(), isTyping });
    }

    private byte[] GetDataKey()
    {
        var encoded = configuration["Security:DataEncryptionKeyBase64"]
            ?? throw new HubException("Data encryption key is missing.");
        var key = Convert.FromBase64String(encoded);
        if (key.Length != 32) throw new HubException("Data encryption key is invalid.");
        return key;
    }

    private Guid CurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new HubException("Invalid user session.");
    }
}
