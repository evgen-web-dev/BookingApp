using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Persistence.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _dbContext;

    public SessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void Add(Session session)
    {
        _dbContext.Set<Session>().Add(session);
    }

    public async Task Revoke(int sessionId, RevocationReason revocationReason)
    {
        await _dbContext.Set<Session>()
            .Where(session =>
                session.Id == sessionId)
            .ExecuteUpdateAsync(setters =>
                {
                    setters.SetProperty(session => session.RevokedAt, DateTime.UtcNow);
                    setters.SetProperty(session => session.RevokedReason, revocationReason);
                }
            );
    }
}