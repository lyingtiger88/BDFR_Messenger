using BDFR.Database.Models;

namespace BDFR.Gateway.Contracts;

public sealed record CreateStoreRequest(
    string Name,
    string? Description,
    StoreKind Kind,
    string? CardNumber,
    string? Iban,
    string? NationalCode,
    string? BirthDate);

public sealed record StoreResponse(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    StoreKind Kind,
    bool IsActive,
    SellerVerificationStatus BankVerificationStatus,
    string? BankName,
    string? CardLast4,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
