using BookingApp.Application.DTOs.Auth;
using FluentValidation;

namespace BookingApp.API.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
   public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();
    }
}