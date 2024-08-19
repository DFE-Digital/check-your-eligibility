// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface ISchoolsSearch
    {
        void RefreshData();
        Task<IEnumerable<School>?> Search(string query);
    }
}