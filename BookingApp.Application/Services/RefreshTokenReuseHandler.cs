using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingApp.Application.Services;

public class RefreshTokenReuseHandler : IRefreshTokenReuseHandler
{
    private readonly ILogger<RefreshTokenReuseHandler> _logger;

    public RefreshTokenReuseHandler(ILogger<RefreshTokenReuseHandler> logger)
    {
        _logger = logger;
    }
    
    public Task<OperationResult> HandleReuseAsync(int tokenId)
    {
        _logger.LogWarning(
            "Refresh token reuse detected, tokenId is {TokenId} (in {ClassName}.{MethodName})", 
            tokenId,
            nameof(RefreshTokenReuseHandler),
            nameof(HandleReuseAsync));
     
        // TODO: complete "force-user-password-change" flow on token-reuse detect
        
        return Task.FromResult(OperationResult.Success());
    }
}