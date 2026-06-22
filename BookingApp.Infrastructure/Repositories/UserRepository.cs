using BookingApp.Domain;
using BookingApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _appDbContext;

    public UserRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    
    public async Task<User> CreateUser(User user, CancellationToken cancellationToken = default)
    {
        var newUser = _appDbContext.Users.Add(user);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return newUser.Entity;
    }

    public async Task<User?> GetUserById(int id, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
}