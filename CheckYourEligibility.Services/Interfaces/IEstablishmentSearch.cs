// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IEstablishmentSearch
    {
        Task<IEnumerable<Establishment>?> Search(string query);
    }
}