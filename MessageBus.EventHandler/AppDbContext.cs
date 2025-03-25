using MessageBus.IntegrationEventLog.EF;
using MessageBus.IntegrationEventLog.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MessageBus.EventHandler;

public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Toast> Toasts { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.UseIntegrationEventLogs();
    }
}

public class Order : IEntity<Guid>
{
    [Key]
    public Guid Id { get; set; } = new();
    [Required]
    public required string Data { get; set; }
}
public class Toast : IEntity<int>
{
    [Key]
    public int Id { get; set; } = new();
    [Required]
    public required string Data { get; set; }
}