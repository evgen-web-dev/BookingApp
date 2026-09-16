using BookingApp.API.Validators.Pagination;
using BookingApp.Application.DTOs.Booking;
using FluentValidation;

namespace BookingApp.API.Validators.Bookings;

public class MyBookingsRequestValidator : AbstractValidator<MyBookingsPaginatedRequest>
{
    public MyBookingsRequestValidator()
    {
        Include(new PaginatedRequestValidator());
    }
}