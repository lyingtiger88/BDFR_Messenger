using System.Security.Claims;
using BDFR.Database;
using BDFR.Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Gateway.Realtime;

[Authorize]
public sealed class ChatHub(MessengerDbContext db) : Hub
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
            Content = content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var dto = new
        {
            message.Id,
            message.SenderId,
            message.RecipientId,
            message.Content,
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

    private Guid CurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new HubException("Invalid user session.");
    }
}
