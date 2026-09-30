using BDFR.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Database;

public sealed class MessengerDbContext(DbContextOptions<MessengerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();

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

        var session = modelBuilder.Entity<Session>();
        session.HasKey(x => x.Id);
        session.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsRequired();
        session.HasIndex(x => x.RefreshTokenHash).IsUnique();
        session.HasOne(x => x.User)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
