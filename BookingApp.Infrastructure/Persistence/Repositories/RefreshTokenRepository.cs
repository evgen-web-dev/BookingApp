using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void Add(RefreshToken token)
    {
        _dbContext.Set<RefreshToken>().Add(token);
    }

    public async Task<RevokeOutcome> Revoke(int tokenId)
    {
        /*
        This will be executed on the DB as
        "UPDATE ... WHERE id = @id AND revoked_at IS NULL" SQL query
        which will return affectedRowsAmount = 0 only for a case when refresh token of current user
        was already revoked with using this method.
        So, as long as this can not happen during "normal" conditions - we assume token-reuse for affectedRowsAmount = 0
        */
        var affectedRowsAmount = await _dbContext.Set<RefreshToken>()
            .Where(token => 
                token.Id == tokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => 
                setters.SetProperty(token => token.RevokedAt, DateTime.UtcNow));
        
        return affectedRowsAmount == 0
            ? RevokeOutcome.IsAlreadyRevoked
            : RevokeOutcome.RevokedSuccessfully;
    }

    public async Task<int> RevokeAllLiveForSession(int sessionId)
    {
        return await _dbContext.Set<RefreshToken>()
            .Where(token => 
                token.RevokedAt == null && token.SessionId == sessionId)
            .ExecuteUpdateAsync(setters => 
                setters.SetProperty(token => token.RevokedAt, DateTime.UtcNow));
    }

    public async Task<RefreshToken?> FindByHashWithSessionWithoutTracking(string tokenHash)
    {
        return await _dbContext.Set<RefreshToken>()
            .AsNoTracking()
            .Where(token => 
                token.TokenHash == tokenHash)
            .Include(token => 
                token.Session)
            .FirstOrDefaultAsync();
    }
}