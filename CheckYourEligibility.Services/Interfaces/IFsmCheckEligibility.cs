using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<CheckEligibilityItemFsm?> GetItem(string guid);
        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<string> PostCheck(CheckEligibilityRequestDataFsm data);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid);
        Task<ApplicationSave> PostApplication(ApplicationRequestData data);
        Task<ApplicationResponse?> GetApplication(string guid);
        Task<IEnumerable<ApplicationResponse>> GetApplications(ApplicationRequestSearchData model);
        Task<ApplicationStatusUpdateResponse> UpdateApplicationStatus(string guid,ApplicationStatusData data);
    }
}