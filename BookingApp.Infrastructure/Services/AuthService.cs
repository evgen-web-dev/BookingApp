using System.Collections.ObjectModel;
using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;

namespace BookingApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private static readonly IReadOnlyDictionary<string, string?> _authErrorsMap = new Dictionary<string, string?>
    {
        ["PasswordTooShort"] = null,
        ["PasswordRequiresNonAlphanumeric"] = null,
        ["PasswordRequiresDigit"] = null,
        ["PasswordRequiresLower"] = null,
        ["PasswordRequiresUpper"] = null,
        ["PasswordRequiresUniqueChars"] = null,
        ["DuplicateUserName"] = "Invalid username, please try another username",
        ["DuplicateEmail"] = "Invalid email, please try another email",
        ["InvalidUserName"] = "Invalid username, please try another username",
    };
    
    public AuthService(AppDbContext dbContext, UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // TODO: encapsulate GetMappedIdentityErrors into separate class-helper when mapping for IdentityErrors' .Description is needed in multiple places 
    private List<string> GetMappedIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var identityErrors = errors.ToList();
        if (!identityErrors.Any())
        {
            return [];
        }

        List<string> mappedErrors = new List<string>();

        foreach (var error in identityErrors)
        {
            if (!_authErrorsMap.ContainsKey(error.Code))
            {
                continue;
            }
            
            // when we _authErrorsMap[error.Code] is null (for instance, for "Password*" error-codes -
            // means we are putting unchanged text from error.Description field)
            mappedErrors.Add(_authErrorsMap[error.Code] ?? error.Description);
        }
        
        return mappedErrors;
    }
    
    public async Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        /*
        to proceed - "request.Role" must be present in the database AND "request.Role" must be either of (Roles.Client or Roles.Host).
        otherwise - stopping here.
        */
        if (!await _roleManager.RoleExistsAsync(request.Role) || request.Role is not (Roles.Client or Roles.Host))
        {
            return new AuthResult<RegisterResponse>(
                false,
                ["Could not create account", $"Invalid role provided: {request.Role}"]
            );
        }

        User userFromMappedRequest = request.Adapt<User>();
        
        var registerDbContextTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var createNewUserResult = await _userManager.CreateAsync(userFromMappedRequest, request.Password);
        if (!createNewUserResult.Succeeded)
        {
            await registerDbContextTransaction.RollbackAsync(cancellationToken);
            
            var errors = GetMappedIdentityErrors(createNewUserResult.Errors);
            errors.AddRange(["Could not create account"]);
            
            return new AuthResult<RegisterResponse>(false,  errors);
        }
        
        var addUserToRoleResult = await _userManager.AddToRoleAsync(userFromMappedRequest, request.Role);
        if (!addUserToRoleResult.Succeeded)
        {
            await registerDbContextTransaction.RollbackAsync(cancellationToken);
            
            var errors = GetMappedIdentityErrors(createNewUserResult.Errors);
            errors.AddRange(["Could not create account", "Error occurred while adding role to a user"]);
            
            return new AuthResult<RegisterResponse>(false, errors);
        }
            
        await registerDbContextTransaction.CommitAsync(cancellationToken);

        return new AuthResult<RegisterResponse>(
            true,
            null,
            userFromMappedRequest.Adapt<RegisterResponse>()
        );
    }
}