using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace BDFR.Gateway.Realtime;

public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
