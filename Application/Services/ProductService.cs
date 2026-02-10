using AutoMapper;
using ECommerce.Api.Application.Dtos.Products;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using ECommerce.Api.Domain;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Application.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext dbContext, IMapper mapper, ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResponse<GetProductResponse>> CreateProductAsync(CreateProductRequest request)
    {
        var productNameExists = await _dbContext.Products
            .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower());

        if (productNameExists)
        {
            return ServiceResponse<GetProductResponse>.Fail("Product name already exists", 400);
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock
        };

        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        var response = _mapper.Map<GetProductResponse>(product);
        return ServiceResponse<GetProductResponse>.Ok("Product created successfully", response);
    }

    public async Task<ServiceResponse<PagedResponse<GetProductResponse>>> GetProductsAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var products = await _dbContext.Products
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await _dbContext.Products.CountAsync();
        var items = _mapper.Map<List<GetProductResponse>>(products);
        var paged = PagedResponse<GetProductResponse>.Create(items, totalCount, skip, pageSize);

        return ServiceResponse<PagedResponse<GetProductResponse>>.Ok("Products fetched successfully", paged);
    }

    public async Task<ServiceResponse<GetProductResponse>> GetProductByIdAsync(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return ServiceResponse<GetProductResponse>.Fail("Product not found", 404);
        }

        var response = _mapper.Map<GetProductResponse>(product);
        return ServiceResponse<GetProductResponse>.Ok("Product fetched successfully", response);
    }

    public async Task<ServiceResponse<bool>> DeleteProductAsync(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return ServiceResponse<bool>.Fail("Product not found", 404);
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return ServiceResponse<bool>.Ok("Product deleted successfully", true);
    }

    public async Task<ServiceResponse<GetProductResponse>> UpdateStockAsync(Guid id, UpdateStockRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var product = await _dbContext.Products
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return ServiceResponse<GetProductResponse>.Fail("Product not found", 404);
            }

            product.Stock += request.Quantity;
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = _mapper.Map<GetProductResponse>(product);
            return ServiceResponse<GetProductResponse>.Ok("Stock updated successfully", response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to update stock");
            return ServiceResponse<GetProductResponse>.Fail("Failed to update stock", 400);
        }
    }
}
