using BookingApp.Application.DTOs.Common;
using FluentValidation;

namespace BookingApp.API.Validators.Pagination;

public class PaginatedRequestValidator : AbstractValidator<PaginatedRequest>
{
    public PaginatedRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}