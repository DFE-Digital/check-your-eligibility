using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface ICheckEligibility
    {
        Task<IEnumerable<CheckEligibilityItemFsm>> GetBulkCheckResults(string guid);
        Task<BulkStatus?> GetBulkStatus(string guid);
        Task<CheckEligibilityItemFsm?> GetItem(string guid);
        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<PostCheckResult> PostCheck<T>(T data, string? groupId = null);
        Task PostCheck<T>(IEnumerable<T> data, string groupId);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid, AuditData? auditItem);
        Task<CheckEligibilityStatusResponse> UpdateEligibilityCheckStatus(string guid, EligibilityCheckStatusData data);
    }
}