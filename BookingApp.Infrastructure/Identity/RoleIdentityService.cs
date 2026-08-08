using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BookingApp.Infrastructure.Identity;

public class RoleIdentityService : IRoleIdentityService
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public RoleIdentityService(RoleManager<IdentityRole<int>> roleManager)
    {
        _roleManager = roleManager;
    }
    
    public async Task<bool> ExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }

    public async Task<OperationResult> CreateAsync(string roleName)
    {
        var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });

        if (!createRoleResult.Succeeded)
        {
            return OperationResult.Failure(IdentityErrorCodesDefaultDenyMapper.AdaptCreateRoleErrorCodes(createRoleResult.Errors));
        }
        
        return OperationResult.Success();
    }
}