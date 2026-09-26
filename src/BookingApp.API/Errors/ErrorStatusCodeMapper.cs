using BookingApp.Application.Errors;

namespace BookingApp.API.Errors;

public static class ErrorStatusCodeMapper
{
    private static readonly IReadOnlyDictionary<string, int> StatusCodesMap = new Dictionary<string, int>
    {
        [AuthErrorCodes.InvalidEmailOrPassword] = StatusCodes.Status401Unauthorized,
        [AuthErrorCodes.InvalidRefreshToken] = StatusCodes.Status401Unauthorized,
        [AuthErrorCodes.UserNotFound] = StatusCodes.Status401Unauthorized,
        [AuthErrorCodes.InvalidEmailOrUserName] = StatusCodes.Status400BadRequest,
        [GenericErrorCodes.UnexpectedError] = StatusCodes.Status500InternalServerError,
        [BookingErrorCodes.ApartmentNotFound] = StatusCodes.Status404NotFound,
        [BookingErrorCodes.ApartmentNotAvailable] = StatusCodes.Status409Conflict,
        [BookingErrorCodes.InvalidCheckInDate] = StatusCodes.Status400BadRequest,
        [BookingErrorCodes.BookingNotFound] = StatusCodes.Status404NotFound,
    };

    public static int GetStatusCodeForError(string error, int fallbackStatusCode = StatusCodes.Status500InternalServerError)
    {
        return StatusCodesMap.TryGetValue(error, out var statusCode) 
            ? statusCode 
            : fallbackStatusCode;
    }
}