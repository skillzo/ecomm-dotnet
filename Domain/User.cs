namespace ECommerce.Api.Domain;

public class User : BaseEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}


public enum UserRole
{
    Admin,
    Customer
}
