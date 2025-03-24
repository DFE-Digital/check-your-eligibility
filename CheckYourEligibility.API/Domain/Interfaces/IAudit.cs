// Ignore Spelling: Fsm

using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Requests;

namespace CheckYourEligibility.API.Gateways.Interfaces
{
    public interface IAudit
    {
        Task<string> AuditAdd(AuditData auditData);
        AuditData? AuditDataGet(AuditType type, string id);
        Task<string> CreateAuditEntry(AuditType type, string id);
    }
}