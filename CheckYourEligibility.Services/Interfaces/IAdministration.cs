// Ignore Spelling: Fsm

using Microsoft.AspNetCore.Http;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IAdministration
    {
        Task CleanUpEligibilityChecks();
        Task ImportEstablishments(IFormFile file);
    }
}