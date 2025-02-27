using Ardalis.GuardClauses;
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
            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.Administration, string.Empty);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }
        }
    }
}