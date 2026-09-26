namespace BookingApp.API.Errors;

public static class ProblemDetailsTypeStatusCodeMapper
{
    private static readonly string Status500Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
    private static readonly string FallbackType = Status500Type;
    
    private static readonly IReadOnlyDictionary<int, string> ProblemDetailsTypesMap = new Dictionary<int, string>
    {
        [StatusCodes.Status400BadRequest] = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        [StatusCodes.Status401Unauthorized] = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        [StatusCodes.Status404NotFound] = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        [StatusCodes.Status409Conflict] = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        [StatusCodes.Status500InternalServerError] = Status500Type
    };

    public static string GetProblemDetailsTypeForStatusCode(int statusCode)
    {
        return ProblemDetailsTypesMap.TryGetValue(statusCode, out var problemDetailsType) 
            ? problemDetailsType 
            : FallbackType;
    }
}