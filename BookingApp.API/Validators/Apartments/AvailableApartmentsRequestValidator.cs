using BookingApp.Application.DTOs.Apartment;
using FluentValidation;

namespace BookingApp.API.Validators.Apartments;

public class AvailableApartmentsRequestValidator : AbstractValidator<AvailableApartmentsRequest>
{
    public AvailableApartmentsRequestValidator()
    {
        RuleFor(x => x.AvailableFrom)
            .NotEmpty()
            .When(x => x.AvailableTo != null);


        RuleFor(x => x.AvailableTo)
            .NotEmpty()
                .When(x => x.AvailableFrom != null)
            .GreaterThan(x => x.AvailableFrom.GetValueOrDefault().Date)
                .When(x => x.AvailableFrom != null)
                .WithMessage(x 
                    => $"'{nameof(x.AvailableTo)}' must be greater than to '{nameof(x.AvailableFrom)}'.");
    }
}