﻿using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<BulkStatus?> GetBulkStatus(string guid);
        Task<CheckEligibilityItemFsm?> GetItem(string guid);
        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<PostCheckResult> PostCheck(CheckEligibilityRequestDataFsm data, string? groupId = null);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid, AuditData? auditItem);
        Task<CheckEligibilityStatusResponse> UpdateEligibilityCheckStatus(string guid, EligibilityCheckStatusData data);
    }
}