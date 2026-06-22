using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IAppStatusService
{
    Task<AppStatusDto> GetStatusAsync();
}