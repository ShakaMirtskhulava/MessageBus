using System.ComponentModel.DataAnnotations;

namespace MessageBus.Client.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public required string Data { get; set; }
}


public class OrderRequest
{
    public required string Data { get; set; }
}