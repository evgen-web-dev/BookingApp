namespace BookingApp.Domain.Interfaces;

public interface IUserRepository
{
    Task<User> CreateUser(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserById(int id, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmail(string email, CancellationToken cancellationToken = default);
}