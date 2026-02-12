using ECommerce.Api.Domain;

namespace ECommerce.Api.Application.Dtos;

public class GetAllParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty;
    public SortOrder? SortOrder { get; set; } = null;

    public OrderStatus? Status { get; set; } = null;
}


public enum SortOrder
{
    asc,
    desc
}