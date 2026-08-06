using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;

namespace BookingApp.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public SessionService(ISessionRepository sessionRepository, IRefreshTokenRepository refreshTokenRepository)
    {
        _sessionRepository = sessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }
    
    public async Task RevokeSession(int sessionId, RevocationReason reason)
    {
        await _sessionRepository.Revoke(sessionId, reason);
        await _refreshTokenRepository.RevokeAllLiveForSession(sessionId);
    }
}