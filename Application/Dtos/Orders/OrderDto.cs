using System.ComponentModel.DataAnnotations;
using ECommerce.Api.Domain;

namespace ECommerce.Api.Application.Dtos.Orders;


public class CreateOrderRequest
{

    [Required]
    public required CreateOrderItemsRequest[] Items { get; set; }
}

public class CreateOrderItemsRequest
{
    [Required]
    public required Guid ProductId { get; set; }

    [Range(1, 1_000_000)]
    public required int Quantity { get; set; }
}

public class GetOrderResponse
{
    public Guid Id { get; set; }
    public int TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public ICollection<GetOrderItemResponse> OrderItems { get; set; } = new List<GetOrderItemResponse>();
}






public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }

}