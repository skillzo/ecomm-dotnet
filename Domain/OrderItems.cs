namespace ECommerce.Api.Domain;

public class OrderItem : BaseEntity
{
    public required Order Order { get; set; }
    public required Product Product { get; set; }
    public required int Quantity { get; set; }
    public required int Price { get; set; }
}