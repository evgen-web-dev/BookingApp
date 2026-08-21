using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Errors;
using BookingApp.Application.Exceptions.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Domain.Entities;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IMapper _mapper;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ITokenFamilyService _tokenFamilyService;
    private readonly ITokenFamilyRepository _tokenFamilyRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenRevoker _refreshTokenRevoker;
    private readonly IOptions<TokenFamilyOptions> _tokenFamilyOptions;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(
        IUnitOfWork unitOfWork, 
        IUserIdentityService userIdentityService, 
        IMapper mapper,
        IAccessTokenService accessTokenService, 
        ITokenFamilyService tokenFamilyService, 
        ITokenFamilyRepository tokenFamilyRepository, 
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRevoker refreshTokenRevoker,
        IRefreshTokenRepository refreshTokenRepository, 
        IOptions<TokenFamilyOptions> tokenFamilyOptions,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _userIdentityService = userIdentityService;
        _mapper = mapper;
        _accessTokenService = accessTokenService;
        _tokenFamilyService = tokenFamilyService;
        _tokenFamilyRepository = tokenFamilyRepository;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenRevoker = refreshTokenRevoker;
        _tokenFamilyOptions = tokenFamilyOptions;
        _logger = logger;
    }
    
    public async Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        User userFromMappedRequest = _mapper.Map<User>(request);
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var createUserResult = await _userIdentityService.CreateAsync(userFromMappedRequest, request.Password);
            if (!createUserResult.Succeeded)
            {
                await SafeRollbackAsync(cancellationToken);
                return OperationResult<RegisterResponse>.Failure(createUserResult.Errors);
            }

            var assignUserToRole = await _userIdentityService.AddToRoleAsync(userFromMappedRequest, request.Role);
            if (!assignUserToRole.Succeeded)
            {
                await SafeRollbackAsync(cancellationToken);
                return OperationResult<RegisterResponse>.Failure(assignUserToRole.Errors);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return OperationResult<RegisterResponse>.Success(new RegisterResponse(createUserResult.Value.Id));
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                await SafeRollbackAsync(cancellationToken);
            }
            
            throw;
        }
    }

    public async Task<OperationResult<IssuedTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var authenticatedUserResult = await _userIdentityService.AuthenticateAsync(request.Email, request.Password);

        if (!authenticatedUserResult.Succeeded)
        {
            return OperationResult<IssuedTokens>.Failure(authenticatedUserResult.Errors);
        }
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var newTokenFamily = new TokenFamily
            {
                UserId = authenticatedUserResult.Value.Id,
                AbsoluteExpiresAt = DateTime.UtcNow.AddDays(_tokenFamilyOptions.Value.TokenFamilyAbsoluteLifeTimeDays),
                CreatedAt = DateTime.UtcNow
            };
        
            _tokenFamilyRepository.Add(newTokenFamily);
        
            var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
            if (!_refreshTokenService.TryHashRefreshToken(newRawRefreshToken, out var newRefreshTokenHash))
            {
                throw new InvalidRefreshTokenHashGenerationException();
            }
        
            var newRefreshTokenObj = new RefreshToken
            {
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenFamilyOptions.Value.RefreshTokenLifeTimeDays),
                TokenHash = newRefreshTokenHash,
                TokenFamily =  newTokenFamily
            };
        
            _refreshTokenRepository.Add(newRefreshTokenObj);
            
            await _unitOfWork.CommitAsync(cancellationToken);
            
            var accessToken = _accessTokenService.GenerateAccessToken(authenticatedUserResult.Value);

            return OperationResult<IssuedTokens>.Success(new IssuedTokens(newRawRefreshToken, accessToken));
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown when rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }
            
            throw;
        }
    }
    
    public async Task<OperationResult<IssuedTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (!_refreshTokenService.TryHashRefreshToken(refreshToken, out var refreshTokenHash))
        {
            return OperationResult<IssuedTokens>.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }
        
        var currentRefreshTokenObj = await _refreshTokenRepository.FindByHashWithTokenFamilyWithoutTracking(refreshTokenHash);

        if (currentRefreshTokenObj is null)
        {
            return OperationResult<IssuedTokens>.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        var isTokenFamilyExpired = currentRefreshTokenObj.ExpiresAt < DateTime.UtcNow || currentRefreshTokenObj.TokenFamily.AbsoluteExpiresAt < DateTime.UtcNow;

        try
        {
            if (isTokenFamilyExpired)
            {
                await _tokenFamilyService.RevokeTokenFamily(currentRefreshTokenObj.TokenFamilyId, RevocationReason.Expired);
                await _unitOfWork.CommitAsync(cancellationToken);
                
                return OperationResult<IssuedTokens>.Failure([AuthErrorCodes.InvalidRefreshToken]);
            }

            var currentRefreshTokenRevocationResult = await _refreshTokenRevoker.RevokeAsync(currentRefreshTokenObj.Id);

            if (currentRefreshTokenRevocationResult is RevokeOutcome.IsAlreadyRevoked)
            {
                await _tokenFamilyService.RevokeTokenFamily(currentRefreshTokenObj.TokenFamilyId, RevocationReason.TheftDetected);
                await _unitOfWork.CommitAsync(cancellationToken);
                
                return OperationResult<IssuedTokens>.Failure([AuthErrorCodes.InvalidRefreshToken]);
            }

            var newRawRefreshToken = _refreshTokenService.GenerateRefreshToken();
            if (!_refreshTokenService.TryHashRefreshToken(newRawRefreshToken, out var newRefreshTokenHash))
            {
                throw new InvalidRefreshTokenHashGenerationException();
            }

            _refreshTokenRepository.Add(new RefreshToken
            {
                TokenFamilyId = currentRefreshTokenObj.TokenFamilyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_tokenFamilyOptions.Value.RefreshTokenLifeTimeDays),
                TokenHash = newRefreshTokenHash,
            });

            var currentUserDataResult = await _userIdentityService.GetWithRolesById(currentRefreshTokenObj.TokenFamily.UserId);
            if (!currentUserDataResult.Succeeded)
            {
                await SafeRollbackAsync(cancellationToken);
                return OperationResult<IssuedTokens>.Failure([AuthErrorCodes.InvalidRefreshToken]);
            }

            var newAccessToken = _accessTokenService.GenerateAccessToken(currentUserDataResult.Value);

            await _unitOfWork.CommitAsync(cancellationToken);
            
            return OperationResult<IssuedTokens>.Success(new IssuedTokens(newRawRefreshToken, newAccessToken));
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown when rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }
            
            throw;
        }
    }

    public async Task<OperationResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (!_refreshTokenService.TryHashRefreshToken(refreshToken, out var currentRefreshTokenHash))
        {
            return OperationResult.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }

        var currentRefreshTokenObj = await _refreshTokenRepository.FindByHashWithTokenFamilyWithoutTracking(currentRefreshTokenHash);

        if (currentRefreshTokenObj is null)
        {
            return OperationResult.Failure([AuthErrorCodes.InvalidRefreshToken]);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var currentRefreshTokenRevocationOutcome = await _refreshTokenRevoker.RevokeAsync(currentRefreshTokenObj.Id);

            var tokenFamilyRevocationReason = currentRefreshTokenRevocationOutcome is RevokeOutcome.IsAlreadyRevoked
                ? RevocationReason.TheftDetected
                : RevocationReason.Logout;

            var operationResult = currentRefreshTokenRevocationOutcome is RevokeOutcome.IsAlreadyRevoked
                ? OperationResult.Failure([AuthErrorCodes.InvalidRefreshToken])
                : OperationResult.Success();

            await _tokenFamilyService.RevokeTokenFamily(currentRefreshTokenObj.TokenFamilyId, tokenFamilyRevocationReason);

            await _unitOfWork.CommitAsync(cancellationToken);

            return operationResult;
        }
        catch
        {
            if (_unitOfWork.HasActiveTransaction)
            {
                // SafeRollbackAsync will catch and "silence" exception if it was thrown when rolling back a transaction
                await SafeRollbackAsync(cancellationToken);
            }
            
            throw;
        }
    }
    
    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed transaction rollback in {ClassName}.{Method}",
                nameof(AuthService),
                nameof(SafeRollbackAsync));
        }
    }
}