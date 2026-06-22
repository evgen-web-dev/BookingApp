using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;

namespace BookingApp.Application.Services;

public class AppStatusService: IAppStatusService
{
    public async Task<AppStatusDto> GetStatusAsync()
    {
        return new AppStatusDto { Status = "running", Version = "0.0.1" };
    }
}