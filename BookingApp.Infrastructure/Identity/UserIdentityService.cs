using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Errors;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Microsoft.AspNetCore.Identity;

namespace BookingApp.Infrastructure.Identity;

public class UserIdentityService : IUserIdentityService
{
    private readonly UserManager<User> _userManager;

    public UserIdentityService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }
    
    public async Task<OperationResult<CreateUserResult>> CreateAsync(User user, string password)
    {
        var createNewUserResult = await _userManager.CreateAsync(user, password);
        if (!createNewUserResult.Succeeded)
        {
            return OperationResult<CreateUserResult>.Failure(
                IdentityErrorCodesDefaultDenyMapper.AdaptRegisterUserErrorCodes(createNewUserResult.Errors)
            );
        }
        
        return OperationResult<CreateUserResult>.Success(new CreateUserResult(user.Id));
    }

    public async Task<OperationResult> AddToRoleAsync(User user, string role)
    {
        var addRoleToUser = await _userManager.AddToRoleAsync(user, role);
        if (!addRoleToUser.Succeeded)
        {
            return OperationResult.Failure(
                IdentityErrorCodesDefaultDenyMapper.AdaptAddUserToRoleErrorCodes(addRoleToUser.Errors)
            );
        }
        
        return OperationResult.Success();
    }

    public async Task<OperationResult> VerifyCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return OperationResult.Failure([]);
        }
        
        return OperationResult.Success();
    }

    public async Task<OperationResult<AuthenticatedUserResult>> AuthenticateAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        // TODO - refactor later to check for lockout as well
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return OperationResult<AuthenticatedUserResult>.Failure([AuthErrorCodes.InvalidEmailOrPassword]);
        }
        
        var userRoles = await _userManager.GetRolesAsync(user);
        
        return OperationResult<AuthenticatedUserResult>.Success(
            new AuthenticatedUserResult(
                user.Id, 
                user.Email ?? throw new InvalidOperationException("Authenticated user has no email on record"), 
                userRoles.ToList()
            )
        );
    }
}