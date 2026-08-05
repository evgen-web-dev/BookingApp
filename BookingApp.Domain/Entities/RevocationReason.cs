namespace BookingApp.Domain.Entities;

public enum RevocationReason
{
    LogOut,
    TheftDetected,
    AbsoluteExpiry,
    SlidingExpiry
}