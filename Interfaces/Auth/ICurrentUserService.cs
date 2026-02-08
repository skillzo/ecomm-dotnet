// Services/ICurrentUserService.cs
using ECommerce.Api.Domain;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Task<User?> GetCurrentUserAsync(CancellationToken ct = default);
}