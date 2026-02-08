namespace ECommerce.Api.Domain;

public class Order : BaseEntity
{
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }


    public Guid UserId { get; set; }
    public required User User { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}


public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}