namespace BookingApp.Application.DTOs.Common;

public abstract record PaginatedRequest
{
    public int PageSize { get; init; } = 5;
    public int PageNumber { get; init; } = 1;
}