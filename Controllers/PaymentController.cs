



using ECommerce.Api.Application.Dtos.Payments;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PaymentController> _logger;
    public PaymentController(IPaymentService paymentService, ICurrentUserService currentUserService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("initiate")]
    public async Task<ActionResult<ServiceResponse<InitializePaymentResponse>>> InitializePayment([FromBody] InitializePaymentRequest request)

    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return BadRequest(ServiceResponse<InitializePaymentResponse>.Fail("User not found", 400));
        }
        var result = await _paymentService.InitializePaymentAsync(request, userId.Value);
        return Ok(result);
    }


    [HttpGet("verify")]
    public async Task<ActionResult<ServiceResponse<VerifyPaymentResponse>>> VerifyPayment([FromQuery] string reference)
    {
        _logger.LogInformation("reference is: {@reference}", reference);
        var result = await _paymentService.VerifyPaymentAsync(reference);
        return Ok(result);
    }


}