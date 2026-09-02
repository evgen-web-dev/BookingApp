using BookingApp.Infrastructure.Identity;
using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using BookingApp.Infrastructure.Services;
using BookingApp.Infrastructure.Persistence;
using BookingApp.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IRoleIdentityService, RoleIdentityService>();
        services.AddScoped<IAccessTokenService, JsonWebTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        
        return services;
    }
    
    public static IServiceCollection AddInfrastructurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITokenFamilyRepository, TokenFamilyRepository>();
        
        return services;
    }
}