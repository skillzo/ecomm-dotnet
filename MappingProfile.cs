using AutoMapper;
using ECommerce.Api.Domain;
using ECommerce.Api.Dtos.Products;

namespace ECommerce.Api;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, GetProductResponse>();
    }
}
