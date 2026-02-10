using ECommerce.Api.Domain;

namespace ECommerce.Api.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Task<User?> GetCurrentUserAsync(CancellationToken ct = default);
}
