using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Persistence.Repositories;

public class TokenFamilyRepository : ITokenFamilyRepository
{
    private readonly AppDbContext _dbContext;

    public TokenFamilyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void Add(TokenFamily tokenFamily)
    {
        _dbContext.Set<TokenFamily>().Add(tokenFamily);
    }

    public async Task Revoke(int tokenFamilyId, RevocationReason revocationReason)
    {
        await _dbContext.Set<TokenFamily>()
            .Where(tokenFamily =>
                tokenFamily.Id == tokenFamilyId)
            .ExecuteUpdateAsync(setters =>
                {
                    setters.SetProperty(tokenFamily => tokenFamily.RevokedAt, DateTime.UtcNow);
                    setters.SetProperty(tokenFamily => tokenFamily.RevokedReason, revocationReason);
                }
            );
    }
}