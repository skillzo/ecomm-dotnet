using AutoMapper;
using ECommerce.Api.Application.Dtos;
using ECommerce.Api.Application.Dtos.Orders;
using ECommerce.Api.Application.Interfaces;
using ECommerce.Api.Common;
using ECommerce.Api.Domain;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
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

    public async Task<ServiceResponse<PagedResponse<OrderDto>>> GetAllOrdersAsync(GetAllParams request)
    {
        var query = _dbContext.Orders.AsQueryable();
        var skip = (request.Page - 1) * request.PageSize;

        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(o => o.OrderItems
            .Any(oi => EF.Functions.ILike(oi.Product.Name, $"%{request.Search}%")));
        }

        if (request.Status != null)
        {
            query = query.Where(o => o.Status == request.Status);
        }


        query = request.Sort?.ToLower() switch
        {
            "orderdate" => request.SortOrder == SortOrder.desc
            ? query.OrderByDescending(o => o.OrderDate)
            : query.OrderBy(o => o.OrderDate),


            "status" => request.SortOrder == SortOrder.desc
            ? query.OrderByDescending(o => o.Status)
            : query.OrderBy(o => o.Status),


            _ => query.OrderByDescending(o => o.OrderDate)
        };




        var orders = await query
            .Skip(skip)
            .Take(request.PageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                Status = o.Status.ToString(),
                OrderDate = o.OrderDate,
            })
            .ToListAsync();

        var totalCount = await query.CountAsync();
        var pagedResponse = PagedResponse<OrderDto>.Create(orders, totalCount, skip, request.PageSize);
        return ServiceResponse<PagedResponse<OrderDto>>
        .Ok("Orders fetched successfully", pagedResponse);
    }
}
