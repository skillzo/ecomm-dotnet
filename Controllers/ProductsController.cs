using ECommerce.Api.Application.Dtos.Products;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [Authorize(Roles = nameof(Domain.UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<ServiceResponse<GetProductResponse>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var result = await _productService.CreateProductAsync(request);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<PagedResponse<GetProductResponse>>>> GetProducts(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var result = await _productService.GetProductsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<GetProductResponse>>> GetProduct(Guid id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteProduct(Guid id)
    {
        var result = await _productService.DeleteProductAsync(id);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    [Authorize(Roles = nameof(Domain.UserRole.Admin))]
    [HttpPost("{id}/stock")]
    public async Task<ActionResult<ServiceResponse<GetProductResponse>>> UpdateStock(
        Guid id, 
        [FromBody] UpdateStockRequest request)
    {
        var result = await _productService.UpdateStockAsync(id, request);
        
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }
}
