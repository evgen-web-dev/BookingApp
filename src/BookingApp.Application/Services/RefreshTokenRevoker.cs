using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;

namespace BookingApp.Application.Services;

public class RefreshTokenRevoker : IRefreshTokenRevoker
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenReuseHandler _refreshTokenReuseHandler;

    public RefreshTokenRevoker(IRefreshTokenRepository refreshTokenRepository, IRefreshTokenReuseHandler refreshTokenReuseHandler)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenReuseHandler = refreshTokenReuseHandler;
    }
    
    public async Task<RevokeOutcome> RevokeAsync(int tokenId)
    {
        var affectedRowsAmount = await _refreshTokenRepository.Revoke(tokenId);
        
        var revokeTokenOutcome = affectedRowsAmount == 0
            ? RevokeOutcome.IsAlreadyRevoked
            : RevokeOutcome.RevokedSuccessfully;
        
        if (revokeTokenOutcome is RevokeOutcome.IsAlreadyRevoked)
        {
            await _refreshTokenReuseHandler.HandleReuseAsync(tokenId);
        }
        
        return revokeTokenOutcome;
    }
}