using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Errors;

public static class ErrorCodesProblemDetailsFactory
{
    private const string ErrorsProblemDetailsExtensionKey = "errorDetails";
    
    public static ProblemDetails Create(string type, int statusCode, ICollection<string> errors, string? path = null, string? title = null)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Type = type,
            Status = statusCode,
            Instance = path,
            Extensions = new Dictionary<string, object?>
            {
                { ErrorsProblemDetailsExtensionKey, errors }
            }
        };
        
        return problemDetails;
    }
}