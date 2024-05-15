// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IAudit
    {
        Task<string> AuditAdd(AuditData auditData);
    }
}