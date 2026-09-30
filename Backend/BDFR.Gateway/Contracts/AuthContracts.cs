namespace BDFR.Gateway.Contracts;

public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DeviceName,
    string? Platform);

public sealed record LoginRequest(
    string Login,
    string Password,
    string? DeviceName,
    string? Platform);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt);
