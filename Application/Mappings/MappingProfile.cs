using AutoMapper;
using ECommerce.Api.Application.Dtos.Orders;
using ECommerce.Api.Application.Dtos.Products;
using ECommerce.Api.Domain;

namespace ECommerce.Api.Application.Mappings;

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
