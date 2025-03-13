using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface ISearchApplicationsUseCase
    {
        Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model, string? localAuthorityId = null);
    }

    public class SearchApplicationsUseCase : ISearchApplicationsUseCase
    {
        private readonly IApplication _applicationService;
        private readonly IAudit _auditService;

        public SearchApplicationsUseCase(IApplication applicationService, IAudit auditService)
        {
            _applicationService = Guard.Against.Null(applicationService);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model, string? localAuthorityId = null)
        {
            var response = await _applicationService.GetApplications(model);

            if (response == null || !response.Data.Any())
            {
                return null;
            }
            await _auditService.CreateAuditEntry(AuditType.Administration, string.Empty);
            
            return response;
        }
    }
}