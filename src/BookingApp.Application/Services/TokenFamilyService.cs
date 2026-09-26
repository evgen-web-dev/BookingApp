using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;

namespace BookingApp.Application.Services;

public class TokenFamilyService : ITokenFamilyService
{
    private readonly ITokenFamilyRepository _tokenFamilyRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenFamilyService(ITokenFamilyRepository tokenFamilyRepository, IRefreshTokenRepository refreshTokenRepository)
    {
        _tokenFamilyRepository = tokenFamilyRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }
    
    public async Task RevokeTokenFamily(int tokenFamilyId, RevocationReason reason)
    {
        await _tokenFamilyRepository.Revoke(tokenFamilyId, reason);
        await _refreshTokenRepository.RevokeAllLiveForTokenFamily(tokenFamilyId);
    }
}