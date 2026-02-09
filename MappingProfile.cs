using AutoMapper;
using ECommerce.Api.Domain;
using ECommerce.Api.Dtos.Orders;
using ECommerce.Api.Dtos.Products;

namespace ECommerce.Api;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, GetProductResponse>();
        CreateMap<Order, GetOrderResponse>()
        .ForMember(d => d.TotalPrice,
        o => o.MapFrom(s => s.OrderItems.Sum(oi => oi.Price * oi.Quantity)));
        CreateMap<OrderItem, GetOrderItemResponse>();
    }
}
