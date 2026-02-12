using ECommerce.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<User> Users { get; set; }


    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e => e.HasKey(p => p.Id));
        modelBuilder.Entity<Order>(e => e.HasKey(o => o.Id));
        modelBuilder.Entity<OrderItem>(e => e.HasKey(oi => oi.Id));
        modelBuilder.Entity<User>(e => e.HasKey(u => u.Id));
        modelBuilder.Entity<Payment>(e => e.HasKey(p => p.Id));
    }
}