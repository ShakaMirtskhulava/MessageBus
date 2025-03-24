using MessageBus.IntegrationEventLog;
using System.ComponentModel.DataAnnotations;

namespace MessageBus.Client.Models;

public class Order : IEntity<Guid>
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public required string Data { get; set; }
}


public class OrderRequest
{
    public required string Data { get; set; }
}

public class Toast : IEntity<int>
{
    [Key]
    public int Id { get; set; } = new();
    [Required]
    public required string Data { get; set; }
}

public class ToastRequest
{
    public required string Data { get; set; }
}