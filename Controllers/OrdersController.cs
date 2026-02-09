



using AutoMapper;
using ECommerce.Api.Domain;
using ECommerce.Api.Dtos.Orders;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<OrdersController> _logger;


    private readonly ICurrentUserService _currentUserService;

    public OrdersController(
        AppDbContext dbContext,
        IMapper mapper,
        ILogger<OrdersController> logger,
        ICurrentUserService currentUserService
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }


    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpPost]
    public async Task<ActionResult<GetOrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            Console.WriteLine($"User details here {user}");
            if (user == null)
                return BadRequest("User not found");

            var productIds = request.Items.Select(item => item.ProductId).ToArray();
            var products = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != request.Items.Length)
                return BadRequest("Invalid Product Present in your selection");

            foreach (var item in request.Items)
            {
                var product = products.Single(p => p.Id == item.ProductId);
                if (product.Stock < item.Quantity)
                    return BadRequest($"Insufficient stock for {product.Name}");
                product.Stock -= item.Quantity;
            }

            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                UserId = user.Id,
                User = user
            };
            foreach (var oi in request.Items)
            {
                var product = products.Single(p => p.Id == oi.ProductId);
                order.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = product.Price
                });
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new GetOrderResponse
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                TotalPrice = order.OrderItems.Sum(oi => oi.Price * oi.Quantity),
                OrderItems = order.OrderItems.Select(oi => new GetOrderItemResponse
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };




            return Ok(new { userId = user.Id, order = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            await transaction.RollbackAsync();
            return BadRequest(new { error = "Failed to create order" });
        }
    }



    //  get all order (Admin)
    [Authorize]
    [HttpGet]
    public async Task<ActionResult> GetAllOrder()

    {
        var orders = await _dbContext.Orders
        .Select(o => new
        {
            o.Id,
            o.UserId,
            Status = o.Status.ToString(),
            o.OrderDate,
        }).ToListAsync();

        return Ok(orders);
    }

    // get my order
    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet]
    [Route("my-orders")]
    public async Task<ActionResult<List<GetOrderResponse>>> GetMyOrders()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (user == null)
            return BadRequest("User not found");



        var orders = await _dbContext.Orders
        .Where(o => o.UserId == user.Id)
        .Select(o => new GetOrderResponse
        {
            Id = o.Id,
            Status = o.Status.ToString(),
            OrderDate = o.OrderDate,
            TotalPrice = o.OrderItems.Sum(oi => oi.Price * oi.Quantity),
            OrderItems = o.OrderItems.Select(i => new GetOrderItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price

            }).ToList()
        }).ToListAsync();


        return Ok(orders);

    }
}