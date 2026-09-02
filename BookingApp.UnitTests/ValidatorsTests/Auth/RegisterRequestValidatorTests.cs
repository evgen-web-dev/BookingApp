using BookingApp.API.Validators.Auth;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Domain;
using FluentValidation.TestHelper;

namespace BookingApp.UnitTests.ValidatorsTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    public static IEnumerable<TheoryDataRow<DateOnly>> InvalidBirthDates =>
    [
        new (DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18).AddDays(1))),
        new (DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-200))),
    ];
    
    public static IEnumerable<TheoryDataRow<DateOnly>> ValidBirthDates =>
    [
        new (DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18))),
        new (DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-200).AddDays(1))),
    ];
    
    [Theory]
    [InlineData(Roles.Client)]
    [InlineData(Roles.Host)]
    public void Role_WhenInWhiteList_ShouldNotHaveValidatorError(string role)
    {
        var registerRequest = BuildRegisterRequest(role);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.Role);
    }
    
    [Theory]
    [InlineData("client")]
    [InlineData("Hostt")]
    public void Role_WhenNotInWhiteList_ShouldHaveValidatorError(string role)
    {
        var registerRequest = BuildRegisterRequest(role: role);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.Role);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("test@")]
    [InlineData("test@@")]
    [InlineData("testemail")]
    [InlineData("@com")]
    [InlineData("test.com")]
    public void Email_WhenInvalid_ShouldHaveValidatorError(string? email)
    {
        var registerRequest = BuildRegisterRequest(email: email);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.Email);
    }
    
    [Theory]
    [InlineData("test@com")]
    [InlineData("test@example.com")]
    // Deliberately "permissive": FluentValidation's default EmailAddress() mode only checks for a single '@'
    // with non-empty text on both sides - no length or domain-shape check
    [InlineData("test@b")]
    [InlineData("test@b.c")]
    public void Email_WhenValid_ShouldNotHaveValidatorError(string email)
    {
        var registerRequest = BuildRegisterRequest(email: email);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.Email);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Password_WhenEmptyOrHasWhitespacesOnly_ShouldHaveValidatorError(string? password)
    {
        var registerRequest = BuildRegisterRequest(password: password);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }
    
    [Fact]
    public void Password_WhenValid_ShouldNotHaveValidatorError()
    {
        var registerRequest = BuildRegisterRequest(password: "Pa$$word1");
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    [InlineData("1Jack")]
    [InlineData("Jack1")]
    [InlineData("Jack_")]
    [InlineData("Jack_2")]
    [InlineData("Jack@Jack")]
    [InlineData("Jack,Jack")]
    [InlineData("Jack_Jack")]
    [InlineData("Jack!Jack")]
    [InlineData("Jack:Jack")]
    [InlineData("Jack.Jack")]
    [InlineData("SomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250Characters")]
    public void FirstName_WhenInvalid_ShouldHaveValidatorError(string? firstName)
    {
        var registerRequest = BuildRegisterRequest(firstName: firstName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
    }
    
    [Theory]
    [InlineData("John")]
    [InlineData("O'Brian")]
    [InlineData("Jack-Jack")]
    // Deliberately allowing string (including multiple consecutive) spec-symbols because names can be messy
    [InlineData("Jack----Jack")]
    [InlineData("Jack JR.")]
    public void FirstName_WhenValid_ShouldNotHaveValidatorError(string? firstName)
    {
        var registerRequest = BuildRegisterRequest(firstName: firstName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.FirstName);
    }
    
    
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    [InlineData("1Jack")]
    [InlineData("Jack1")]
    [InlineData("Jack_")]
    [InlineData("Jack_2")]
    [InlineData("Jack@Jack")]
    [InlineData("Jack,Jack")]
    [InlineData("Jack_Jack")]
    [InlineData("Jack!Jack")]
    [InlineData("Jack:Jack")]
    [InlineData("Jack.Jack")]
    [InlineData("SomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250Characters")]
    public void LastName_WhenInvalid_ShouldHaveValidatorError(string? lastName)
    {
        var registerRequest = BuildRegisterRequest(lastName: lastName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.LastName);
    }
    
    [Theory]
    [InlineData("John")]
    [InlineData("O'Brian")]
    [InlineData("Jack-Jack")]
    // Deliberately allowing string (including multiple consecutive) spec-symbols because names can be messy
    [InlineData("Jack----Jack")]
    [InlineData("Jack JR.")]
    public void LastName_WhenValid_ShouldNotHaveValidatorError(string? lastName)
    {
        var registerRequest = BuildRegisterRequest(lastName: lastName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.LastName);
    }
    
    [Theory]
    [InlineData("  ")]
    [InlineData("1Jack")]
    [InlineData("Jack1")]
    [InlineData("Jack_")]
    [InlineData("Jack_2")]
    [InlineData("Jack@Jack")]
    [InlineData("Jack,Jack")]
    [InlineData("Jack_Jack")]
    [InlineData("Jack!Jack")]
    [InlineData("Jack:Jack")]
    [InlineData("Jack.Jack")]
    [InlineData("SomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250CharactersSomeVeryVeryVeryLongNameWhichIsdLongerThan250Characters")]
    public void MiddleName_WhenInvalid_ShouldHaveValidatorError(string? middleName)
    {
        var registerRequest = BuildRegisterRequest(middleName: middleName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.MiddleName);
    }
    
    [Theory]
    // MiddleName is optional so "" or null are valid values for it
    [InlineData("")]
    [InlineData(null)]
    [InlineData("John")]
    [InlineData("O'Brian")]
    [InlineData("Jack-Jack")]
    // Deliberately allowing string (including multiple consecutive) spec-symbols because names can be messy
    [InlineData("Jack----Jack")]
    [InlineData("Jack JR.")]
    public void MiddleName_WhenValid_ShouldNotHaveValidatorError(string? middleName)
    {
        var registerRequest = BuildRegisterRequest(middleName: middleName);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.MiddleName);
    }
    
    [Theory]
    [MemberData(nameof(ValidBirthDates))]
    public void DateOfBirth_WhenWithinAllowedRange_ShouldNotHaveValidatorError(DateOnly birthDate)
    {
        var registerRequest = BuildRegisterRequest(dateOfBirth: birthDate);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.DateOfBirth);
    }
    
    [Theory]
    [MemberData(nameof(InvalidBirthDates))]
    public void DateOfBirth_WhenOutOfAllowedRange_ShouldHaveValidatorError(DateOnly birthDate)
    {
        var registerRequest = BuildRegisterRequest(dateOfBirth: birthDate);
        
        var result = _validator.TestValidate(registerRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.DateOfBirth);
    }
    
    private static RegisterRequest BuildRegisterRequest(string? role = null, string? email = null, string? password = null, string? firstName = null, string? lastName = null, string? middleName = null, DateOnly dateOfBirth = default)
    {
        return new RegisterRequest
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            DateOfBirth = dateOfBirth,
            Email = email!,
            Password = password!,
            Role = role!,
        };
    } 
}