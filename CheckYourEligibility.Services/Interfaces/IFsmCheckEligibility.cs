// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<CheckEligibilityStatusResponse?> GetStatus(string guid);
        Task<string> PostCheck(CheckEligibilityRequestDataFsm data);
        Task<CheckEligibilityStatusResponse?> Process(string guid);
    }
}