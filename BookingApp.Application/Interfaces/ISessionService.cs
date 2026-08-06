using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface ISessionService
{
    Task RevokeSession(int sessionId, RevocationReason reason);
}