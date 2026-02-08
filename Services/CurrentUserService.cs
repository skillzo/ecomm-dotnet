// Services/CurrentUserService.cs
using System.Security.Claims;
using ECommerce.Api.Domain;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public Guid? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public async Task<User?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (UserId is null) return null;
        return await _dbContext.Users.FindAsync([UserId.Value], ct);
    }
}