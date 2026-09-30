using Benton.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Benton.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<OrderSession> OrderSessions => Set<OrderSession>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique username
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Unique Role Name
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // Foreign Key Relationships
        modelBuilder.Entity<User>()
            .HasOne(u => u.RoleEntity)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OrderSession>()
            .HasOne(s => s.CreatedByUser)
            .WithMany(u => u.CreatedSessions)
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(o => o.OrderSession)
            .WithMany(s => s.OrderItems)
            .HasForeignKey(o => o.OrderSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(o => o.User)
            .WithMany(u => u.OrderItems)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>()
            .HasOne(o => o.MenuItem)
            .WithMany()
            .HasForeignKey(o => o.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
