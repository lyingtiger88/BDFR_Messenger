namespace BDFR.Database.Models;

public enum StoreKind
{
    Products = 0,
    Services = 1,
    ProductsAndServices = 2
}

public enum SellerVerificationStatus
{
    Pending = 0,
    Verified = 1,
    Rejected = 2
}

public sealed class MarketplaceStore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public StoreKind Kind { get; set; } = StoreKind.Products;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User Owner { get; set; } = null!;
    public SellerBankVerification? BankVerification { get; set; }
}
