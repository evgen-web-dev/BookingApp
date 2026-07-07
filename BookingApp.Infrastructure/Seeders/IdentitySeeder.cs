using BookingApp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Infrastructure.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        if (!await roleManager.RoleExistsAsync(Roles.Host))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = Roles.Host });   
        }
        
        if (!await roleManager.RoleExistsAsync(Roles.Client))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = Roles.Client });   
        }
    }
}