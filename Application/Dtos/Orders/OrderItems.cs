namespace ECommerce.Api.Application.Dtos.Orders;

public class GetOrderItemResponse
{
    public Guid Id { get; set; }
    public required Guid ProductId { get; set; }
    public required int Quantity { get; set; }
    public required int Price { get; set; }
}