namespace ECommerce.Api.Domain;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required int Price { get; set; }

    public int Stock { get; set; } = 0;
}