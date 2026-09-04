namespace BookingApp.Application.DTOs.Common;

public record PaginatedResponse<T>
{
    private PaginatedResponse() { }

    public static PaginatedResponse<T> Create(List<T> items, PageQueryParams queryParams, int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            PageNumber = queryParams.PageNumber,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount
        };
    }
    
    public List<T> Items { get; init; } = [];
    public int PageSize { get; init; }
    public int PageNumber { get; init; }
    public int TotalCount { get; init; }
}