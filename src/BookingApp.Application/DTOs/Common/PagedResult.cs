namespace BookingApp.Application.DTOs.Common;

public record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
}