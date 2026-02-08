namespace ECommerce.Api.Domain;

public class OrderItem : BaseEntity
{
    public required Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public required Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public required int Quantity { get; set; }
    public required int Price { get; set; }
}