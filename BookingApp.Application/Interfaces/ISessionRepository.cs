using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface ISessionRepository
{
    void Add(Session session);
    Task Revoke(int sessionId, RevocationReason revocationReason);
}