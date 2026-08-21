using System.Text.Json;
using BookingApp.API.Errors;
using BookingApp.API.ExceptionHandlers;
using BookingApp.API.Filters;
using BookingApp.Application.DTOs;
using BookingApp.Application.Errors;
using BookingApp.Application.Options.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingApp.API;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrEmpty(options.Issuer), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)} is  missing in configuration")
            .Validate(options => !string.IsNullOrEmpty(options.Audience), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)} missing in configuration")
            .Validate(options => !string.IsNullOrEmpty(options.SigningKey), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} missing in configuration")
            .Validate(options => options.AccessTokenLifetimeMinutes > 0, 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.AccessTokenLifetimeMinutes)} has invalid value")
            .ValidateOnStart();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var jwt =  jwtOptions.Value;
                
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(jwt.SigningKey)
                    ),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
        
        return services;
    }

    public static IServiceCollection AddExceptionHandlersWithProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<AppExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }

    public static IServiceCollection AddAppValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);
        return services;
    }

    public static void AddAppFilters(this FilterCollection filterCollection)
    {
        filterCollection.Add<AsyncValidationFilter>();
    }

    public static ValidationProblemDetails ToValidationProblemDetails(this ValidationResult result, 
        string path, 
        string? title = "One or more validation errors occurred.")
    {
        return new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Type = ProblemDetailsTypeStatusCodeMapper.GetProblemDetailsTypeForStatusCode(StatusCodes.Status400BadRequest),
            Errors = result.ToDictionary(),
            Instance = path,
            Title = title
        };
    }
    
    public static ActionResult ToProblemDetailsResult(this OperationResult result, string path, string? title = null)
    {
        string errorForDefiningStatusCode = result.Errors.Count > 0 
            ? result.Errors[0]
            : GenericErrorCodes.UnexpectedError;
        int errorStatusCode = ErrorStatusCodeMapper.GetStatusCodeForError(errorForDefiningStatusCode, StatusCodes.Status400BadRequest);
        string problemDetailsType = ProblemDetailsTypeStatusCodeMapper.GetProblemDetailsTypeForStatusCode(errorStatusCode);
        
        ProblemDetails problemDetails = ErrorCodesProblemDetailsFactory.Create(
            problemDetailsType, 
            errorStatusCode, 
            result.Errors.Count > 0 ? result.Errors.ToList() : [errorForDefiningStatusCode],
            path,
            title);

        return new ObjectResult(problemDetails);
    }
}