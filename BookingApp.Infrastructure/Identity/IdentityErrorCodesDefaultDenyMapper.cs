using BookingApp.Application.Errors;
using Microsoft.AspNetCore.Identity;

namespace BookingApp.Infrastructure.Identity;

public static class IdentityErrorCodesDefaultDenyMapper
{
    private static readonly IReadOnlyDictionary<string, string?> _genericErrorCodesMap = new Dictionary<string, string?>
    {
        ["ConcurrencyFailure"] = null,
        ["DefaultError"] = null
    };
    
    private static readonly IReadOnlyDictionary<string, string?> _registerUserErrorCodesMap = new Dictionary<string, string?>
    {
        ["PasswordTooShort"] = null,
        ["PasswordRequiresNonAlphanumeric"] = null,
        ["PasswordRequiresDigit"] = null,
        ["PasswordRequiresLower"] = null,
        ["PasswordRequiresUpper"] = null,
        ["PasswordRequiresUniqueChars"] = null,
        ["DuplicateUserName"] = AuthErrorCodes.InvalidEmailOrUserName,
        ["DuplicateEmail"] = AuthErrorCodes.InvalidEmailOrUserName,
        ["InvalidUserName"] = AuthErrorCodes.InvalidEmailOrUserName
    };
    
    private static readonly IReadOnlyDictionary<string, string?> _assignUserToRoleErrorCodesMap = new Dictionary<string, string?>
    {
        ["UserAlreadyInRole"] = null
    };
    
    private static List<string> AdaptIdentityErrorCodes(IEnumerable<IdentityError> errors, IReadOnlyDictionary<string, string?> scopedDefaultDenyMap)
    {
        var mappedErrorCodes = new List<string>();

        foreach (var error in errors)
        {
            // either adding "overriden" error-code like for ["DuplicateUserName"] = AuthErrorCodes.InvalidEmailOrUserName,
            // or
            // adding error.Code directly like for ["PasswordTooShort"] = null,
            
            string? mappedErrorCode;
            
            if (scopedDefaultDenyMap.TryGetValue(error.Code, out mappedErrorCode))
            {
                mappedErrorCodes.Add(mappedErrorCode ?? error.Code);
                continue;
            }
            
            if (_genericErrorCodesMap.TryGetValue(error.Code, out mappedErrorCode))
            {
                mappedErrorCodes.Add(mappedErrorCode ?? error.Code);
            }
        }
        
        return mappedErrorCodes;
    }

    public static List<string> AdaptRegisterUserErrorCodes(IEnumerable<IdentityError> errors)
    {
        return AdaptIdentityErrorCodes(errors, _registerUserErrorCodesMap);
    }
    
    public static List<string> AdaptAddUserToRoleErrorCodes(IEnumerable<IdentityError> errors)
    {
        return AdaptIdentityErrorCodes(errors, _assignUserToRoleErrorCodesMap);
    }
}