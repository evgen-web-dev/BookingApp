using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface ITokenFamilyRepository
{
    void Add(TokenFamily tokenFamily);
    Task Revoke(int tokenFamilyId, RevocationReason revocationReason);
}