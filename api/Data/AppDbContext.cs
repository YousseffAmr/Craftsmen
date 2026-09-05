using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Craft> Crafts => Set<Craft>();
    public DbSet<Craftsman> Craftsmen => Set<Craftsman>();
    public DbSet<CraftsmanCraft> CraftsmanCrafts => Set<CraftsmanCraft>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Craft>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name).IsRequired();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role)
                .HasConversion<string>();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Craftsman>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.ContactInfo);
            entity.Property(c => c.DailyRate).HasPrecision(18, 2);
            entity.Property(c => c.LastCheckedAt);
            entity.Property(c => c.Status)
                .HasConversion<string>();
            entity.HasIndex(c => c.UserId).IsUnique();
            entity.HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Craftsman>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CraftsmanCraft>(entity =>
        {
            entity.HasKey(cc => new { cc.CraftsmanId, cc.CraftId });
            entity.HasOne(cc => cc.Craftsman)
                .WithMany(c => c.CraftsmanCrafts)
                .HasForeignKey(cc => cc.CraftsmanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cc => cc.Craft)
                .WithMany(c => c.CraftsmanCrafts)
                .HasForeignKey(cc => cc.CraftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.ContactInfo);
            entity.Property(c => c.LastCheckedAt);
            entity.HasIndex(c => c.UserId).IsUnique();
            entity.HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.Property(r => r.Status).HasConversion<string>();
            entity.Property(r => r.Price).HasPrecision(18, 2);
            entity.Property(r => r.Description).IsRequired();
            entity.HasOne(r => r.Customer)
                .WithMany(c => c.Requests)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Craftsman)
                .WithMany(c => c.Requests)
                .HasForeignKey(r => r.CraftsmanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Craft)
                .WithMany(c => c.Requests)
                .HasForeignKey(r => r.CraftId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(r => new { r.CraftsmanId, r.NeededOn })
                .IsUnique()
                .HasFilter("Status = 'Accepted'");
        });
    }
}
