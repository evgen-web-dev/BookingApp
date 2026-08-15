using BookingApp.Application.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.ExceptionHandlers;

public class AppExceptionHandler : IExceptionHandler
{
    private const string ErrorsProblemDetailsExtensionKey = "errorCodes";
    private readonly IProblemDetailsService _problemDetailsService;

    public AppExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // TODO: implement ILogger logging
        Console.WriteLine(exception);
            
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Status = StatusCodes.Status500InternalServerError,
            Instance = httpContext.Request.Path.Value
        };
            
        problemDetails.Extensions.Add(ErrorsProblemDetailsExtensionKey, new [] { GenericErrorCodes.UnexpectedError });

        try
        {
            await _problemDetailsService.WriteAsync(
                new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problemDetails, Exception = exception });
        }
        catch (Exception writeException)
        {
            // TODO: implement ILogger logging
            Console.WriteLine(writeException);

            try
            {
                if (!httpContext.Response.HasStarted)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await httpContext.Response.WriteAsync(GenericErrorCodes.UnexpectedError, cancellationToken);
                }
            }
            catch (Exception innerWriteException)
            {
                // TODO: implement ILogger logging
                Console.WriteLine(innerWriteException);
            }
        }
        
        return true;
    }
}