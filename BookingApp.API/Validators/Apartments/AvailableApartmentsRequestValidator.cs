using BookingApp.API.Validators.Pagination;
using BookingApp.Application.DTOs.Apartment;
using FluentValidation;

namespace BookingApp.API.Validators.Apartments;

public class AvailableApartmentsRequestValidator : AbstractValidator<AvailableApartmentsPaginatedRequest>
{
    public AvailableApartmentsRequestValidator()
    {
        Include(new PaginatedRequestValidator());
        
        
        RuleFor(x => x.AvailableFrom)
            .NotEmpty()
            .When(x => x.AvailableTo != null);


        RuleFor(x => x.AvailableTo)
            .NotEmpty()
            .When(x => x.AvailableFrom != null);
        
        
        RuleFor(x => x)
            .Must(x => x.AvailableTo.GetValueOrDefault().Date > x.AvailableFrom.GetValueOrDefault().Date)
            .When(x => x.AvailableFrom != null && x.AvailableTo != null)
            .WithName(nameof(AvailableApartmentsPaginatedRequest.AvailableTo))
            .WithMessage(x 
                => $"'{nameof(AvailableApartmentsPaginatedRequest.AvailableTo)}' must be greater than to '{nameof(AvailableApartmentsPaginatedRequest.AvailableFrom)}'.");
    }
}