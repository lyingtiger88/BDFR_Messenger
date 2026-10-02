using BDFR.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Database;

public sealed class MessengerDbContext(DbContextOptions<MessengerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MarketplaceStore> MarketplaceStores => Set<MarketplaceStore>();
    public DbSet<SellerBankVerification> SellerBankVerifications => Set<SellerBankVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.HasKey(x => x.Id);
        user.Property(x => x.Username).HasMaxLength(64).IsRequired();
        user.HasIndex(x => x.Username).IsUnique();
        user.Property(x => x.EmailEncrypted).IsRequired();
        user.Property(x => x.EmailLookupHash).HasMaxLength(64).IsRequired();
        user.HasIndex(x => x.EmailLookupHash).IsUnique();
        user.Property(x => x.PasswordHash).IsRequired();
        user.Property(x => x.IsSellerVerified).IsRequired();
        user.HasIndex(x => x.IsSellerVerified);

        var session = modelBuilder.Entity<Session>();
        session.HasKey(x => x.Id);
        session.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
        session.HasIndex(x => x.RefreshTokenHash).IsUnique();
        session.HasOne(x => x.User)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var store = modelBuilder.Entity<MarketplaceStore>();
        store.HasKey(x => x.Id);
        store.Property(x => x.Name).HasMaxLength(120).IsRequired();
        store.Property(x => x.Description).HasMaxLength(2000);
        store.HasIndex(x => x.OwnerUserId).IsUnique();
        store.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        var bankVerification = modelBuilder.Entity<SellerBankVerification>();
        bankVerification.HasKey(x => x.Id);
        bankVerification.Property(x => x.CardNumberEncrypted).HasMaxLength(512);
        bankVerification.Property(x => x.IbanEncrypted).HasMaxLength(512);
        bankVerification.Property(x => x.CardLast4).HasMaxLength(4);
        bankVerification.Property(x => x.BankName).HasMaxLength(120);
        bankVerification.Property(x => x.Provider).HasMaxLength(120);
        bankVerification.HasIndex(x => x.StoreId).IsUnique();
        bankVerification.HasOne(x => x.Store)
            .WithOne(x => x.BankVerification)
            .HasForeignKey<SellerBankVerification>(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        var message = modelBuilder.Entity<Message>();
        message.HasKey(x => x.Id);
        message.Property(x => x.ContentEncrypted).IsRequired();
        message.HasIndex(x => new { x.SenderId, x.RecipientId, x.CreatedAt });
        message.HasIndex(x => new { x.RecipientId, x.ReadAt });
    }
}
