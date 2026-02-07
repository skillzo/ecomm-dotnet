using System.ComponentModel.DataAnnotations;
using ECommerce.Api.Domain;

namespace ECommerce.Api.Dtos.Orders;

public class CreateOrderRequest
{
    [Required]
    public required Guid ProductId { get; set; }

    [Range(1, 1_000_000)]
    public required int Quantity { get; set; }
}

public class GetOrderResponse
{
    public Guid Id { get; set; }
    public required Guid ProductId { get; set; }
    public required int Quantity { get; set; }

    public int TotalPrice { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public ICollection<GetOrderItemResponse> OrderItems { get; set; } = new List<GetOrderItemResponse>();
}