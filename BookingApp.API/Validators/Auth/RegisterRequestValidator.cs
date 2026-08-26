using BookingApp.Application.DTOs.Auth;
using BookingApp.Domain;
using FluentValidation;

namespace BookingApp.API.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const int NameMaxLength = 250;
    private const int NameMinLength = 1;
    /*
     NameCharactersRegExpPattern checks if sting starts from any-language letter
     and allows only any-language letter + some special chars for different kins of apostrophes and dashes.
     
     Strings this regexp will accept:
      - "John"
      - "O'Brian"
      - "Jack-Jack"
      - "Jack---Jack" --> deliberately allowing string multiple consecutive spec-symbols because names can be messy
      
    Strings this regexp will not accept:
      - "1Jack"
      - "Jack1"
      - "Jack_"
      - "Jack_2"
      - "Jack@Jack"
      - "Jack.Jack"
      - "Jack,Jack"
      - "Jack_Jack"
      - "Jack!Jack"
      - "Jack:Jack"
      - etc
     */
    
    /*
    TODO: confirm that allowing "." (dot) as a spec character inside of the name in NameCharactersRegExpPattern
    is intentional and not a bug.
    If the correctness of allowing "." (dot) as a spec character inside of the name in NameCharactersRegExpPattern
    is confirmed - then remove "Jack.Jack" from "Strings this regexp will not accept" list.
     */
    private const string NameCharactersRegExpPattern = @"^\p{L}[\p{L}'’ʼ`´.\-–—\s]*$";
    
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(NameMinLength)
            .MaximumLength(NameMaxLength)
            .Matches(NameCharactersRegExpPattern);

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(NameMinLength)
            .MaximumLength(NameMaxLength)
            .Matches(NameCharactersRegExpPattern);

        RuleFor(x => x.MiddleName)
            .Cascade(CascadeMode.Stop)
            .MinimumLength(NameMinLength)
                .When(x => !string.IsNullOrEmpty(x.MiddleName))
            .MaximumLength(NameMaxLength)
                .When(x => !string.IsNullOrEmpty(x.MiddleName))
            .Matches(NameCharactersRegExpPattern)
                .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(x => x.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .Must(dateOfBirth => dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-200))
                .WithMessage("Your age must be under 200 years.")
            .Must(dateOfBirth => dateOfBirth <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18))
            .WithMessage("You must be 18 at least years old.");

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(role => Roles.RolesAvailableForPublicRegistration.Contains(role))
                .WithMessage($"Role must be one of the following: {string.Join(", ", Roles.RolesAvailableForPublicRegistration)}");
    }
}