using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Infrastructure.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        
        var roleManager = scope.ServiceProvider.GetRequiredService<IRoleIdentityService>();

        foreach (var roleName in Roles.AllRoles)
        {
            if (await roleManager.ExistsAsync(roleName)) 
                continue;
            
            var seedRoleResult = await roleManager.CreateAsync(roleName);
            if (!seedRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to see a role: {roleName}: {string.Join(", ", seedRoleResult.Errors)}");
            }
        }
    }
}