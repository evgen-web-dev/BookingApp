using BookingApp.Application.DTOs.Booking;
using FluentValidation;

namespace BookingApp.API.Validators.Bookings;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ApartmentId)
            .GreaterThan(0);

        RuleFor(x => x.CheckIn)
            .NotEmpty();
        
        RuleFor(x => x.CheckOut)
            .NotEmpty();
        
        RuleFor(x => x)
            .Must(x => x.CheckOut.Date > x.CheckIn.Date)
                .When(x => x.CheckIn.Date != default && x.CheckOut.Date != default)
                .WithName(nameof(CreateBookingRequest.CheckOut))
                .WithMessage(x 
                    => $"'{nameof(CreateBookingRequest.CheckOut)}' must be greater than '{nameof(CreateBookingRequest.CheckIn)}'.");
    }
}