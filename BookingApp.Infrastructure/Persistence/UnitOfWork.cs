using BookingApp.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookingApp.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly AppDbContext _dbContext;
    private IDbContextTransaction? _dbContextTransaction;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public bool HasActiveTransaction => _dbContextTransaction != null;

    private async Task DisposeDbContextTransaction()
    {
        if (_dbContextTransaction == null)
        {
            return;
        }
        
        await _dbContextTransaction.DisposeAsync();
        _dbContextTransaction = null;
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_dbContextTransaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }
        
        _dbContextTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (_dbContextTransaction == null)
        {
            throw new InvalidOperationException("Could not save changes: transaction has not been started yet");
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_dbContextTransaction == null)
        {
            throw new InvalidOperationException("Could not commit: transaction has not been started yet");
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContextTransaction.CommitAsync(cancellationToken);
        
        await DisposeDbContextTransaction();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_dbContextTransaction == null)
        {
            throw new InvalidOperationException("Could not rollback: transaction has not been started yet");
        }
        
        await _dbContextTransaction.RollbackAsync(cancellationToken);
        
        await DisposeDbContextTransaction();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeDbContextTransaction();
    }
}