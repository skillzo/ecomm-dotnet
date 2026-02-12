using ECommerce.Api.Application.Dtos;
using ECommerce.Api.Application.Dtos.Orders;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using ECommerce.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpPost]
    public async Task<ActionResult<ServiceResponse<GetOrderResponse>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
        {
            return BadRequest(ServiceResponse<GetOrderResponse>.Fail("User not found", 400));
        }

        var result = await _orderService.CreateOrderAsync(request, user.Id);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ServiceResponse<List<object>>>> GetAllOrder([FromQuery] GetAllParams request)
    {
        var result = await _orderService.GetAllOrdersAsync(request);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("my-orders")]
    public async Task<ActionResult<ServiceResponse<List<GetOrderResponse>>>> GetMyOrders()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
        {
            return BadRequest(ServiceResponse<List<GetOrderResponse>>.Fail("User not found", 400));
        }

        var result = await _orderService.GetMyOrdersAsync(user.Id);
        return Ok(result);
    }
}
