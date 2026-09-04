namespace BookingApp.Application.DTOs.Common;

public record PageQueryParams
{
    public required int PageNumber { get; init; }

    public required int PageSize
    {
        get => Math.Min(field, 50); 
        init;
    }
}