// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services.CsvImport;
using Microsoft.AspNetCore.Http;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IAdministration
    {
        Task CleanUpEligibilityChecks();
        Task ImportEstablishments(IEnumerable<EstablishmentRow> data);
        Task ImportHMRCData(IEnumerable<FreeSchoolMealsHMRC> data);
        Task ImportHomeOfficeData(IEnumerable<FreeSchoolMealsHO> data);
    }
}