using System.Text;
using System.Text.Json;
using ECommerce.Api.Application.Dtos.Payments;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using ECommerce.Api.Domain;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    private static readonly JsonSerializerOptions PaystackJsonOptions = new JsonSerializerOptions();

    public PaymentService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServiceResponse<InitializePaymentResponse>> InitializePaymentAsync(
        InitializePaymentRequest request,
        Guid userId)
    {
        try
        {

            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);


            _logger.LogInformation("order data {@order}", order);

            if (order == null)
            {
                return ServiceResponse<InitializePaymentResponse>.Fail("Order not found", 404);
            }

            if (order.Status != OrderStatus.Pending)
            {
                return ServiceResponse<InitializePaymentResponse>.Fail("Order already processed", 400);
            }

            // Calculate total amount
            var totalAmount = order.OrderItems.Sum(oi => oi.Price * oi.Quantity);
            var amountInKobo = totalAmount * 100;

            // Generate reference
            var reference = $"ORDER_{order.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}";


            var paystackResponse = await InitializePaystackTransactionAsync(
                amountInKobo,
                reference,
                userId,
                _configuration["Environment:Paystack:CallbackUrl"]);

            _logger.LogInformation("Paystack response {@paystackResponse}", paystackResponse.Data);

            if (!paystackResponse.Success)
            {
                return ServiceResponse<InitializePaymentResponse>.Fail(
                    paystackResponse.Message,
                    paystackResponse.StatusCode);
            }

            // Create payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                PaymentMethod = "Paystack",
                Status = PaymentStatus.Pending,
                Amount = totalAmount,
                Currency = "NGN",
                TransactionRef = reference,
                TransactionDate = DateTime.UtcNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            return ServiceResponse<InitializePaymentResponse>.Ok(
                "Payment initialized successfully",
                new InitializePaymentResponse
                {
                    AuthorizationUrl = paystackResponse.Data.Data.AuthorizationUrl,
                    Reference = reference
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize payment");
            return ServiceResponse<InitializePaymentResponse>.Fail("Failed to initialize payment", 500);
        }
    }

    public async Task<ServiceResponse<VerifyPaymentResponse>> VerifyPaymentAsync(string reference)
    {
        try
        {
            var payment = await _dbContext.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.TransactionRef == reference);

            if (payment == null)
            {
                return ServiceResponse<VerifyPaymentResponse>.Fail("Payment not found", 404);
            }


            var paystackResponse = await VerifyPaystackTransactionAsync(reference);

            if (!paystackResponse.Success)
            {
                return ServiceResponse<VerifyPaymentResponse>.Fail(
                    paystackResponse.Message,
                    paystackResponse.StatusCode);
            }

            // Update payment status
            payment.Status = PaymentStatus.Success;
            payment.TransactionDate = paystackResponse.Data.Data.TransactionDate;

            // Update order status if payment successful
            if (paystackResponse.Data.Data.Status == "success")
            {
                payment.Order.Status = OrderStatus.Shipped;
            }

            await _dbContext.SaveChangesAsync();

            return ServiceResponse<VerifyPaymentResponse>.Ok(
                "Payment verified successfully",
                new VerifyPaymentResponse
                {
                    Status = PaymentStatus.Success.ToString(),
                    Reference = payment.TransactionRef,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    TransactionDate = payment.TransactionDate
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify payment");
            return ServiceResponse<VerifyPaymentResponse>.Fail("Failed to verify payment", 500);
        }
    }

    public async Task<ServiceResponse<bool>> HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            // Verify webhook signature
            if (!VerifyWebhookSignature(payload, signature))
            {
                return ServiceResponse<bool>.Fail("Invalid webhook signature", 401);
            }

            var webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(payload);
            if (webhookEvent == null || webhookEvent.Event != "charge.success")
            {
                return ServiceResponse<bool>.Ok("Webhook processed", false);
            }

            var reference = webhookEvent.Data.Reference;
            var verifyResult = await VerifyPaymentAsync(reference);

            return ServiceResponse<bool>.Ok("Webhook processed", verifyResult.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle webhook");
            return ServiceResponse<bool>.Fail("Failed to handle webhook", 500);
        }
    }


    private async Task<ServiceResponse<PaystackInitializeResponse>> InitializePaystackTransactionAsync(
        int amount,
        string reference,
        Guid userId,
        string? callbackUrl)
    {
        var client = _httpClientFactory.CreateClient("Paystack");
        var secretKey = _configuration["Environment:Paystack:SecretKey"];

        var requestBody = new
        {
            amount = amount,
            email = await GetUserEmailAsync(userId),
            reference = reference,
            callback_url = callbackUrl ?? _configuration["Environment:Paystack:CallbackUrl"]
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

        var response = await client.PostAsync("transaction/initialize", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse<PaystackInitializeResponse>.Fail(
                "Paystack API error",
                (int)response.StatusCode);
        }

        var paystackResponse = JsonSerializer.Deserialize<PaystackInitializeResponse>(
            responseContent,
            PaystackJsonOptions);

        _logger.LogInformation("Paystack response {@paystackResponse}", paystackResponse);

        return ServiceResponse<PaystackInitializeResponse>.Ok(
            "Transaction initialized",
            paystackResponse!);
    }

    private async Task<ServiceResponse<PaystackVerifyResponse>> VerifyPaystackTransactionAsync(string reference)
    {
        var client = _httpClientFactory.CreateClient("Paystack");
        var secretKey = _configuration["Environment:Paystack:SecretKey"];

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

        var response = await client.GetAsync($"transaction/verify/{reference}");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResponse<PaystackVerifyResponse>.Fail(
                "Paystack API error",
                (int)response.StatusCode);
        }

        var paystackResponse = JsonSerializer.Deserialize<PaystackVerifyResponse>(
            responseContent,
            PaystackJsonOptions);

        return ServiceResponse<PaystackVerifyResponse>.Ok(
            "Transaction verified",
            paystackResponse!);
    }

    private bool VerifyWebhookSignature(string payload, string signature)
    {
        var secretKey = _configuration["Environment:Paystack:SecretKey"];
        var hash = ComputeHMACSHA256(payload, secretKey);
        return hash == signature;
    }

    private string ComputeHMACSHA256(string data, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private async Task<string> GetUserEmailAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user?.Email ?? "";
    }
}