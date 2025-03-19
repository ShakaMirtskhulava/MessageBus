using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MessageBus.IntegrationEventLog.EF;

namespace MessageBus.EventPublisher;

class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.UseIntegrationEventLogs();
    }
}

public class Order
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public required string Data { get; set; }
}
