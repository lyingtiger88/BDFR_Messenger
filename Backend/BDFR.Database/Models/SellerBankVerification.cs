namespace BDFR.Database.Models;

public sealed class SellerBankVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoreId { get; set; }

    // Never store raw card/account numbers.
    public string? CardNumberEncrypted { get; set; }
    public string? IbanEncrypted { get; set; }
    public string? AccountNumberEncrypted { get; set; }
    public string? BankCode { get; set; }
    public string? CardLast4 { get; set; }
    public string? BankName { get; set; }

    public SellerVerificationStatus Status { get; set; } = SellerVerificationStatus.Pending;
    public string? Provider { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MarketplaceStore Store { get; set; } = null!;
}
