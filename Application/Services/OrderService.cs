using AutoMapper;
using ECommerce.Api.Application.Dtos.Orders;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using ECommerce.Api.Domain;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Application.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext dbContext, IMapper mapper, ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResponse<GetOrderResponse>> CreateOrderAsync(CreateOrderRequest request, Guid userId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return ServiceResponse<GetOrderResponse>.Fail("User not found", 400);
            }

            var productIds = request.Items.Select(item => item.ProductId).ToArray();
            var products = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != request.Items.Length)
            {
                return ServiceResponse<GetOrderResponse>.Fail("Invalid Product Present in your selection", 400);
            }

            foreach (var item in request.Items)
            {
                var product = products.Single(p => p.Id == item.ProductId);
                if (product.Stock < item.Quantity)
                {
                    return ServiceResponse<GetOrderResponse>.Fail($"Insufficient stock for {product.Name}", 400);
                }
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

            // Reload order with OrderItems for mapping
            var createdOrder = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            var response = _mapper.Map<GetOrderResponse>(createdOrder!);
            response.Status = createdOrder!.Status.ToString();

            return ServiceResponse<GetOrderResponse>.Ok("Order created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            await transaction.RollbackAsync();
            return ServiceResponse<GetOrderResponse>.Fail("Failed to create order", 400);
        }
    }

    public async Task<ServiceResponse<List<GetOrderResponse>>> GetMyOrdersAsync(Guid userId)
    {
        var orders = await _dbContext.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ToListAsync();

        var response = orders.Select(o => new GetOrderResponse
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
        }).ToList();

        return ServiceResponse<List<GetOrderResponse>>.Ok("Orders fetched successfully", response);
    }

    public async Task<ServiceResponse<List<object>>> GetAllOrdersAsync()
    {
        var orders = await _dbContext.Orders
            .Select(o => new
            {
                o.Id,
                o.UserId,
                Status = o.Status.ToString(),
                o.OrderDate,
            })
            .ToListAsync();

        var response = orders.Cast<object>().ToList();
        return ServiceResponse<List<object>>.Ok("Orders fetched successfully", response);
    }
}
