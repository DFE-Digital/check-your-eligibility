// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<string> PostCheck(CheckEligibilityRequestData data);
    }
}