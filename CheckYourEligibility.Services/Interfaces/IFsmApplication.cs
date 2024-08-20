using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmApplication
    {
        Task<ApplicationResponse> PostApplication(ApplicationRequestData data);
        Task<ApplicationResponse?> GetApplication(string guid);
        Task<ApplicationSearchResponse> GetApplications(ApplicationRequestSearch model);
        Task<ApplicationStatusUpdateResponse> UpdateApplicationStatus(string guid, ApplicationStatusData data);
    }
}