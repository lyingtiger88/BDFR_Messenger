namespace BDFR.Gateway.Contracts;

public sealed record UserProfileResponse(
    Guid Id,
    string Username,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    bool IsActive,
    bool IsSellerVerified,
    string? VerificationBadge,
    bool HasStore);
