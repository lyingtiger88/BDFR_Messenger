using BDFR.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BDFR.Database;

public sealed class MessengerDbContext(DbContextOptions<MessengerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.HasKey(x => x.Id);
        user.Property(x => x.Username).HasMaxLength(64).IsRequired();
        user.HasIndex(x => x.Username).IsUnique();
        user.Property(x => x.EmailEncrypted).IsRequired();
        user.Property(x => x.PasswordHash).IsRequired();
    }
}
