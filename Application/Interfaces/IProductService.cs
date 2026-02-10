using ECommerce.Api.Application.Dtos.Products;
using ECommerce.Api.Common;

namespace ECommerce.Api.Application.Interfaces;

public interface IProductService
{
    Task<ServiceResponse<GetProductResponse>> CreateProductAsync(CreateProductRequest request);
    Task<ServiceResponse<PagedResponse<GetProductResponse>>> GetProductsAsync(int page, int pageSize);
    Task<ServiceResponse<GetProductResponse>> GetProductByIdAsync(Guid id);
    Task<ServiceResponse<bool>> DeleteProductAsync(Guid id);
    Task<ServiceResponse<GetProductResponse>> UpdateStockAsync(Guid id, UpdateStockRequest request);
}
