// Ignore Spelling: Fsm

using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Requests;

namespace CheckYourEligibility.API.Gateways.Interfaces
{
    public interface IHash
    {
        Task<EligibilityCheckHash?> Exists(CheckProcessData item);
        Task<string> Create(CheckProcessData item, CheckEligibilityStatus checkResult, ProcessEligibilityCheckSource source, AuditData auditDataTemplate);
    }
}