// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IHash
    {
        Task<EligibilityCheckHash?> Exists(EligibilityCheck item);
        Task<string> Create(EligibilityCheck item, CheckEligibilityStatus checkResult, ProcessEligibilityCheckSource source, AuditData auditDataTemplate);
    }
}