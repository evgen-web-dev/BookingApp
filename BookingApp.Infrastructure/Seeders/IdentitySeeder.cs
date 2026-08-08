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

        if (!await roleManager.ExistsAsync(Roles.Host))
        {
            await roleManager.CreateAsync(Roles.Host);
        }
        
        if (!await roleManager.ExistsAsync(Roles.Client))
        {
            await roleManager.CreateAsync(Roles.Client);
        }
    }
}