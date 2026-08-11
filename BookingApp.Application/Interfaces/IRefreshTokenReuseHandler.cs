using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IRefreshTokenReuseHandler
{
    Task<OperationResult> HandleReuseAsync(int tokenId);
}