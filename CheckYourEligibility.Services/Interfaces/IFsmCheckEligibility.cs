using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<CheckEligibilityItemFsm?> GetItem(string guid);
        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<PostCheckResult> PostCheck(CheckEligibilityRequestDataFsm data);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid, AuditData? auditItem);
        Task<CheckEligibilityStatusResponse> UpdateEligibilityCheckStatus(string guid, EligibilityCheckStatusData data);
    }
}