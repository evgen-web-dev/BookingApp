using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;

namespace BookingApp.Application.Services;

public class RefreshTokenReuseHandler : IRefreshTokenReuseHandler
{
    public Task<OperationResult> HandleReuseAsync(int tokenId)
    {
        // TODO: complete "force-user-password-change" flow on token-reuse detect
        return Task.FromResult(OperationResult.Success());
    }
}