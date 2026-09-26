namespace BookingApp.Application.Interfaces;

public interface IUnitOfWork
{
    bool HasActiveTransaction { get; }
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}