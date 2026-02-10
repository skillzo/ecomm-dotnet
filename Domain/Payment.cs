namespace ECommerce.Api.Domain;

public class Payment : BaseEntity
{
    public required string PaymentMethod { get; set; }
    public required string Status { get; set; }
    public required int Amount { get; set; }
    public required string Currency { get; set; }
    public required string TransactionRef { get; set; }
    public required DateTime TransactionDate { get; set; }

}