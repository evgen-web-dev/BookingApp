using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Domain;
using Mapster;
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
}