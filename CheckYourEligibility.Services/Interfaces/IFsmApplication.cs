using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmApplication
    {
        Task<ApplicationSave> PostApplication(ApplicationRequestData data);
        Task<ApplicationResponse?> GetApplication(string guid);
        Task<IEnumerable<ApplicationResponse>> GetApplications(ApplicationRequestSearchData model);
        Task<ApplicationStatusUpdateResponse> UpdateApplicationStatus(string guid, ApplicationStatusData data);
    }
}