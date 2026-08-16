using BookingApp.API.Errors;
using BookingApp.Application.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.ExceptionHandlers;

public class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<AppExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, 
            "Unhandled exception for {Path} in {ClassName}.{MethodName}",
            httpContext.Request.Path.Value, 
            nameof(AppExceptionHandler), 
            nameof(TryHandleAsync));
        
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
            _logger.LogError(
                writeException, 
                "Unhandled exception during write of problem details into HttpContext with {IProblemDetailsService} for {Path} in {ClassName}.{MethodName}", 
                nameof(IProblemDetailsService),
                httpContext.Request.Path.Value,
                nameof(AppExceptionHandler),
                nameof(TryHandleAsync));

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
                _logger.LogError(innerWriteException, 
                    "Unhandled exception during write of problem details into HttpContext with \"httpContext.Response.WriteAsync\" for {Path} in {ClassName}.{MethodName}",
                    httpContext.Request.Path.Value,
                    nameof(AppExceptionHandler),
                    nameof(TryHandleAsync));
            }
        }
        
        return true;
    }
}