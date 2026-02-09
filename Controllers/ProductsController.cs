using AutoMapper;
using ECommerce.Api.Domain;
using ECommerce.Api.Dtos.Products;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext dbContext, IMapper mapper, ILogger<ProductsController> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<GetProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {

        var ProductNameExist = await _dbContext.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower());
        if (ProductNameExist)
        {
            return BadRequest(new { error = "Product name already exists" });
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
        return Ok(new GetProductResponse { Id = product.Id, Name = product.Name, Description = product.Description, Price = product.Price });
    }




    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetProducts()
    {
        var products = await _dbContext.Products.ToListAsync();
        return Ok(_mapper.Map<List<GetProductResponse>>(products));
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<GetProductResponse>> GetProduct(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found" });
        }
        return Ok(_mapper.Map<GetProductResponse>(product));
    }


    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteProduct(Guid id)

    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found" });
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
        return Ok(true);
    }



    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("{id}/stock")]
    public async Task<ActionResult<GetProductResponse>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var product = await _dbContext.Products
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            product.Stock += request.Quantity;
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(_mapper.Map<GetProductResponse>(product));

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to update stock");
            return BadRequest(new { error = "Failed to update stock" });
        }
    }

}