using System.ComponentModel.DataAnnotations;
namespace ECommerce.Api.Dtos.Products;


public class CreateProductRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    [Required]
    [MaxLength(1000)]
    public required string Description { get; set; }
    
    [Range(1, 1_000_000)]
    public required int Price { get; set; }

    [Range(0, 1_000_000)]
    public required int Stock { get; set; }
}


public class GetProductResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Price { get; set; }
}


public class UpdateStockRequest
{
    [Range(10, 100_000)]
    public int Quantity { get; set; }
}