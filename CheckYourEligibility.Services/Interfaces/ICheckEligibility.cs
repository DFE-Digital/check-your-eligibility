using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface ICheckEligibility
    {
        Task<PostCheckResult> PostCheck<T>(T data) where T : CheckEligibilityRequestData_Fsm;
        Task PostCheck<T>(T data, string groupId) where T : IEnumerable<CheckEligibilityRequestData_Fsm>;

        Task<T> GetBulkCheckResults<T>(string guid) where T : IList<CheckEligibilityItem>;
        Task<T?> GetItem<T>(string guid) where T : CheckEligibilityItem;

        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<BulkStatus?> GetBulkStatus(string guid);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid, AuditData? auditItem);
        Task<CheckEligibilityStatusResponse> UpdateEligibilityCheckStatus(string guid, EligibilityCheckStatusData data);
    }
}