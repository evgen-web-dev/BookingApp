using BookingApp.API.Errors;
using BookingApp.Application.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.ExceptionHandlers;

public class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public AppExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // TODO: implement ILogger logging
        Console.WriteLine(exception);
        
        var defaultErrorCode = GenericErrorCodes.UnexpectedError;
        var statusCode = ErrorStatusCodeMapper.GetStatusCodeForError(defaultErrorCode);
        
        var problemDetails = ErrorCodesProblemDetailsFactory.Create(
            ProblemDetailsTypeStatusCodeMapper.GetProblemDetailsTypeForStatusCode(statusCode),
            statusCode,
            [defaultErrorCode],
            httpContext.Request.Path.Value,
            "An unexpected error occurred.");

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