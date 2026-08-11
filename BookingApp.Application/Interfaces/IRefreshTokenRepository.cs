using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<int> Revoke(int tokenId);
    Task<int> RevokeAllLiveForTokenFamily(int tokenFamilyId);
    Task<RefreshToken?> FindByHashWithTokenFamilyWithoutTracking(string tokenHash);
}