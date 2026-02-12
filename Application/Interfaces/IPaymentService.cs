using ECommerce.Api.Common;
using ECommerce.Api.Application.Dtos.Payments;

namespace ECommerce.Api.Application.Interfaces;



public interface IPaymentService
{
    Task<ServiceResponse<InitializePaymentResponse>> InitializePaymentAsync(InitializePaymentRequest request, Guid userId);
    Task<ServiceResponse<VerifyPaymentResponse>> VerifyPaymentAsync(string reference);

    Task<ServiceResponse<bool>> HandleWebhookAsync(string payload, string signature);
}