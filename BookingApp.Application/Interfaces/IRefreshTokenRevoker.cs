using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IRefreshTokenRevoker
{
    Task<RevokeOutcome> RevokeAsync(int tokenId);
}