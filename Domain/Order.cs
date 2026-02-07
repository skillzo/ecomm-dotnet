namespace ECommerce.Api.Domain;

public class Order : BaseEntity
{
    public required Product Product { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }


    public ICollection<OrderItem> OrderItems { get; set; }= new List<OrderItem>();
}


public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}