// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IAdministration
    {
        Task CleanUpEligibilityChecks();
    }
}