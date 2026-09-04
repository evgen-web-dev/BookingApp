using System.Runtime.CompilerServices;
using BookingApp.Application.DTOs.Apartment;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.DTOs.Booking;
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
         
         config.NewConfig<Apartment, ApartmentDetailsResponse>()
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.Title, src => src.Title)
             .Map(dest => dest.Location, src => src.Location)
             .Map(dest => dest.Description, src => src.Description)
             .Map(dest => dest.Capacity, src => src.Capacity)
             .Map(dest => dest.Price, src => src.Price)
             .IgnoreNonMapped(true);
         
         config.NewConfig<CreateBookingRequest, Booking>()
             .Map(dest => dest.CheckIn, src => src.CheckIn)
             .Map(dest => dest.CheckOut, src => src.CheckOut)
             .Map(dest => dest.ApartmentId, src => src.ApartmentId)
             .IgnoreNonMapped(true);
         
         config.NewConfig<Booking, BookingResponse>()
             .Map(dest => dest.Id, src => src.Id)
             .Map(dest => dest.ApartmentId, src => src.ApartmentId)
             .Map(dest => dest.CheckIn, src => src.CheckIn)
             .Map(dest => dest.CheckOut, src => src.CheckOut)
             .Map(dest => dest.CreatedAt, src => src.CreatedAt)
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
        services.AddScoped<IBookingService, BookingService>();
        
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