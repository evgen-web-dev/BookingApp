using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Application.Services;
using BookingApp.Domain.Entities;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Application;

public static class DependencyInjectionExtensions
{
    public static void AddApplicationMapping(this IServiceCollection _)
    {
        TypeAdapterConfig<RegisterRequest, User>
            .NewConfig()
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.UserName, src => src.Email)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.MiddleName, src => src.MiddleName)
            .Map(dest => dest.MiddleName, src => src.MiddleName)
            .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
            .IgnoreNonMapped(true);
        
        TypeAdapterConfig<User, RegisterResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .IgnoreNonMapped(true);
    }

    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
    }
    
    public static void AddUserSessionsOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UserSessionOptions>()
            .Bind(configuration.GetSection(UserSessionOptions.SectionName))
            .Validate(options => options.AbsoluteLifeTimeDays > 0, 
                $"{UserSessionOptions.SectionName}:{nameof(UserSessionOptions.AbsoluteLifeTimeDays)} has invalid value")
            .Validate(options => options.RefreshTokenLifeTimeDays > 0, 
                $"{UserSessionOptions.SectionName}:{nameof(UserSessionOptions.RefreshTokenLifeTimeDays)} has invalid value")
            .ValidateOnStart();
    }
}