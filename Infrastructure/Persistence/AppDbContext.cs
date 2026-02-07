using ECommerce.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e => e.HasKey(p => p.Id));
        modelBuilder.Entity<Order>(e => e.HasKey(o => o.Id));
    }
}