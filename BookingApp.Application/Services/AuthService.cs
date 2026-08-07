using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Errors;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Domain.Entities;
using BookingApp.Domain;
using Mapster;
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

    private void ThrowOnRefreshTokenHashInvalidGeneration()
    {
        throw new InvalidOperationException("Could not generate hash for refresh token");    
    }
    
    public async Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Roles.RolesAvailableForPublicRegistration.Contains(request.Role))
        {
            return OperationResult<RegisterResponse>.Failure([AuthErrorCodes.CouldNotCreateAccount, AuthErrorCodes.InvalidRoleProvided]);
        }

        User userFromMappedRequest = request.Adapt<User>();
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
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

            return OperationResult<RegisterResponse>.Success(new RegisterResponse(createUserResult.Value.Id));
        }
        finally
        {
            if (_unitOfWork.HasActiveTransaction)
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
        if (_refreshTokenService.TryHashRefreshToken(newRawRefreshToken, out var newRefreshTokenHash))
        {
            ThrowOnRefreshTokenHashInvalidGeneration();
        }
        
        var newRefreshTokenObj = new RefreshToken
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_userSessionOptions.Value.RefreshTokenLifeTimeDays),
            TokenHash = newRefreshTokenHash,
            Session =  newSession
        };
        
        _refreshTokenRepository.Add(newRefreshTokenObj);
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        finally
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown whe rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }
        }
        
        var accessToken = _accessTokenService.GenerateAccessToken(authenticatedUserResult.Value);

        return OperationResult<LoginResult>.Success(
            new LoginResult(newRawRefreshToken, new LoginResponse(accessToken))
        );
    }

    private async Task SaveAndCommitChanges(CancellationToken cancellationToken)
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
    }
    
    public async Task<OperationResult<RefreshResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (!_refreshTokenService.TryHashRefreshToken(refreshToken, out var refreshTokenHash))
        {
            return OperationResult<RefreshResult>.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }
        
        var currentRefreshTokenObj = await _refreshTokenRepository.FindByHashWithSessionWithoutTracking(refreshTokenHash);

        if (currentRefreshTokenObj is null)
        {
            return OperationResult<RefreshResult>.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        var isSessionExpired = currentRefreshTokenObj.ExpiresAt < DateTime.UtcNow || currentRefreshTokenObj.Session.AbsoluteExpiresAt < DateTime.UtcNow;

        try
        {
            if (isSessionExpired)
            {
                await _sessionService.RevokeSession(currentRefreshTokenObj.SessionId, RevocationReason.Expired);
                await SaveAndCommitChanges(cancellationToken);
                
                return OperationResult<RefreshResult>.Failure([AuthErrorCodes.InvalidRefreshToken]);
            }

            var currentRefreshTokenRevocationResult = await _refreshTokenRepository.Revoke(currentRefreshTokenObj.Id);

            if (currentRefreshTokenRevocationResult is RevokeOutcome.IsAlreadyRevoked)
            {
                await _sessionService.RevokeSession(currentRefreshTokenObj.SessionId, RevocationReason.TheftDetected);
                await SaveAndCommitChanges(cancellationToken);
                
                return OperationResult<RefreshResult>.Failure([AuthErrorCodes.InvalidRefreshToken]);
            }

            var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
            if (!_refreshTokenService.TryHashRefreshToken(newRawRefreshToken, out var newRefreshTokenHash))
            {
                ThrowOnRefreshTokenHashInvalidGeneration();
            }

            _refreshTokenRepository.Add(new RefreshToken
            {
                SessionId = currentRefreshTokenObj.SessionId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_userSessionOptions.Value.RefreshTokenLifeTimeDays),
                TokenHash = newRefreshTokenHash,
            });

            var currentUserDataResult = await _userIdentityService.GetWithRolesById(currentRefreshTokenObj.Session.UserId);
            if (!currentUserDataResult.Succeeded)
            {
                return OperationResult<RefreshResult>.Failure([AuthErrorCodes.ErrorDuringTokenRefresh]);
            }

            var newAccessToken = _accessTokenService.GenerateAccessToken(currentUserDataResult.Value);

            await SaveAndCommitChanges(cancellationToken);
            
            return OperationResult<RefreshResult>.Success(new RefreshResult(newRawRefreshToken, new RefreshResponse(newAccessToken)));
        }
        finally
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown whe rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }
        }
    }
}