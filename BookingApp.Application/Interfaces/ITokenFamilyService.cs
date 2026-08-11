using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface ITokenFamilyService
{
    Task RevokeTokenFamily(int tokenFamilyId, RevocationReason reason);
}