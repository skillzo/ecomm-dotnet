namespace ECommerce.Api.Application.Dtos.Payments;



public class InitializePaymentRequest
{
    public required Guid OrderId { get; set; }
}


public class InitializePaymentResponse
{
    public required string AuthorizationUrl { get; set; }
    public required string Reference { get; set; }
}

public class VerifyPaymentResponse
{
    public required string Status { get; set; }
    public required string Reference { get; set; }
    public required int Amount { get; set; }
    public required string Currency { get; set; }
    public DateTime TransactionDate { get; set; }
}