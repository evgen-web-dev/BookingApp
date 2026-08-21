using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Application.Services;
using BookingApp.Domain.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationMapping(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        
         config.NewConfig<RegisterRequest, User>()
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.UserName, src => src.Email)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.MiddleName, src => src.MiddleName)
            .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
            .IgnoreNonMapped(true);
        
         config.NewConfig<User, RegisterResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .IgnoreNonMapped(true);
         
         services.AddSingleton<IMapper>(new Mapper(config));
         
         return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRefreshTokenRevoker, RefreshTokenRevoker>();
        services.AddScoped<IRefreshTokenReuseHandler, RefreshTokenReuseHandler>();
        services.AddScoped<ITokenFamilyService, TokenFamilyService>();
        
        return services;
    }
    
    public static IServiceCollection AddTokenFamilyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TokenFamilyOptions>()
            .Bind(configuration.GetSection(TokenFamilyOptions.SectionName))
            .Validate(options => options.TokenFamilyAbsoluteLifeTimeDays > 0, 
                $"{TokenFamilyOptions.SectionName}:{nameof(TokenFamilyOptions.TokenFamilyAbsoluteLifeTimeDays)} has invalid value")
            .Validate(options => options.RefreshTokenLifeTimeDays > 0, 
                $"{TokenFamilyOptions.SectionName}:{nameof(TokenFamilyOptions.RefreshTokenLifeTimeDays)} has invalid value")
            .ValidateOnStart();
        
        return services;
    }
}