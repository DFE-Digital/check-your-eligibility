using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface ICleanUpEligibilityChecksUseCase
    {
        Task Execute();
    }

    public class CleanUpEligibilityChecksUseCase : ICleanUpEligibilityChecksUseCase
    {
        private readonly IAdministration _service;
        private readonly IAudit _auditService;

        public CleanUpEligibilityChecksUseCase(IAdministration service, IAudit auditService)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task Execute()
        {
            await _service.CleanUpEligibilityChecks();
            await _auditService.CreateAuditEntry(AuditType.Administration, string.Empty);
        }
    }
}