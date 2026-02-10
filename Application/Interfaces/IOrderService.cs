using ECommerce.Api.Application.Dtos.Orders;
using ECommerce.Api.Common;

namespace ECommerce.Api.Application.Interfaces;

public interface IOrderService
{
    Task<ServiceResponse<GetOrderResponse>> CreateOrderAsync(CreateOrderRequest request, Guid userId);
    Task<ServiceResponse<List<GetOrderResponse>>> GetMyOrdersAsync(Guid userId);
    Task<ServiceResponse<List<object>>> GetAllOrdersAsync();
}
