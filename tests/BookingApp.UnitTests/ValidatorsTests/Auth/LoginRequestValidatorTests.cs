using BookingApp.API.Validators.Auth;
using BookingApp.Application.DTOs.Auth;
using FluentValidation.TestHelper;

namespace BookingApp.UnitTests.ValidatorsTests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();
    
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
        var loginRequest = BuildLoginRequest(email: email);
        
        var result = _validator.TestValidate(loginRequest);
        
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
        var loginRequest = BuildLoginRequest(email: email);
        
        var result = _validator.TestValidate(loginRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.Email);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Password_WhenEmptyOrHasWhitespacesOnly_ShouldHaveValidatorError(string? password)
    {
        var loginRequest = BuildLoginRequest(password: password);
        
        var result = _validator.TestValidate(loginRequest);
        
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }
    
    [Fact]
    public void Password_WhenValid_ShouldNotHaveValidatorError()
    {
        var loginRequest = BuildLoginRequest(password: "Pa$$word1");
        
        var result = _validator.TestValidate(loginRequest);
        
        result.ShouldNotHaveValidationErrorFor(r => r.Password);
    }
    
    private static LoginRequest BuildLoginRequest(string? email = null, string? password = null)
    {
        return new LoginRequest
        {
            Email = email!,
            Password = password!,
        };
    } 
}