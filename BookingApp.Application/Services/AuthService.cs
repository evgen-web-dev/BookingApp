using System.Net;
using System.Security.Cryptography;
using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Errors;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Domain.Entities;
using BookingApp.Domain;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BookingApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ISessionService _sessionService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOptions<UserSessionOptions> _userSessionOptions;
    
    public AuthService(
        IUnitOfWork unitOfWork, 
        IUserIdentityService userIdentityService, 
        IAccessTokenService accessTokenService, 
        ISessionService sessionService, 
        ISessionRepository sessionRepository, 
        IRefreshTokenService refreshTokenService, 
        IRefreshTokenRepository refreshTokenRepository, 
        IOptions<UserSessionOptions> userSessionOptions)
    {
        _unitOfWork = unitOfWork;
        _userIdentityService = userIdentityService;
        _accessTokenService = accessTokenService;
        _sessionService = sessionService;
        _sessionRepository = sessionRepository;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _userSessionOptions = userSessionOptions;
    }
    
    public async Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Roles.RolesAvailableForPublicRegistration.Contains(request.Role))
        {
            return OperationResult<RegisterResponse>.Failure([AuthErrorCodes.CouldNotCreateAccount, AuthErrorCodes.InvalidRoleProvided]);
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
                await SafeRollbackAsync(cancellationToken);
            }
        }
    }

    public async Task<OperationResult<LoginResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var authenticatedUserResult = await _userIdentityService.AuthenticateAsync(request.Email, request.Password);

        if (!authenticatedUserResult.Succeeded)
        {
            return OperationResult<LoginResult>.Failure(authenticatedUserResult.Errors);
        }
        
        var newSession = new Session
        {
            UserId = authenticatedUserResult.Value.Id,
            AbsoluteExpiresAt = DateTime.UtcNow.AddDays(_userSessionOptions.Value.AbsoluteLifeTimeDays),
            CreatedAt = DateTime.UtcNow
        };
        
        _sessionRepository.Add(newSession);
        
        var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newRefreshTokenObj = new RefreshToken
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_userSessionOptions.Value.RefreshTokenLifeTimeDays),
            TokenHash = _refreshTokenService.HashRefreshToken(newRawRefreshToken),
            Session =  newSession
        };
        
        _refreshTokenRepository.Add(newRefreshTokenObj);
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var isCommitted = false;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            // throw new Exception("TEST_DEBUGGING_EXCEPTION");
            await _unitOfWork.CommitAsync(cancellationToken);
            isCommitted = true;
        }
        finally
        {
            if (!isCommitted)
            {
                await SafeRollbackAsync(cancellationToken);
            }
        }
        
        var accessToken = _accessTokenService.GenerateAccessToken(authenticatedUserResult.Value);

        return OperationResult<LoginResult>.Success(
            new LoginResult(newRawRefreshToken, new LoginResponse(accessToken))
        );
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
        catch (Exception e)
        {
            // TODO: add ILogger _logger logging for storing info about exception thrown during _unitOfWork.RollbackAsync(cancellationToken) 
            Console.WriteLine(e);
        }
    }
}