using Microsoft.EntityFrameworkCore;
using MessageBus.IntegrationEventLog.EF;
using MessageBus.Client.Models;

namespace MessageBus.Client;

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

