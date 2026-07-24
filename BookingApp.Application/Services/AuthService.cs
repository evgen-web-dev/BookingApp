using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Mapster;

namespace BookingApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityService _userIdentityService;
    
    public AuthService(IUnitOfWork unitOfWork, IUserIdentityService userIdentityService)
    {
        _unitOfWork = unitOfWork;
        _userIdentityService = userIdentityService;
    }
    
    public async Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Roles.RolesAvailableForPublicRegistration.Contains(request.Role))
        {
            return OperationResult<RegisterResponse>.Failure(["CouldNotCreateAccount", "InvalidRoleProvided"]);
        }

        User userFromMappedRequest = request.Adapt<User>();
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var isCommitted = false;
        
        try
        {
            var createUserResult = await _userIdentityService.CreateAsync(userFromMappedRequest, request.Password);
            if (!createUserResult.Succeeded)
            {
                return OperationResult<RegisterResponse>.Failure(createUserResult.Errors);
            }

            var assignUserToRole = await _userIdentityService.AddToRoleAsync(userFromMappedRequest, request.Role);
            if (!assignUserToRole.Succeeded)
            {
                return OperationResult<RegisterResponse>.Failure(assignUserToRole.Errors);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            isCommitted = true;

            return OperationResult<RegisterResponse>.Success(new RegisterResponse(createUserResult.Value.Id));
        }
        finally
        {
            if (!isCommitted)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
            }
        }
    }

    public async Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var authenticatedUserResult = await _userIdentityService.AuthenticateAsync(request.Email, request.Password);

        if (!authenticatedUserResult.Succeeded)
        {
            return OperationResult<LoginResponse>.Failure(authenticatedUserResult.Errors);
        }

        return OperationResult<LoginResponse>.Success(new LoginResponse());
    }
}