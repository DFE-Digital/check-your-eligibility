using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface ISearchApplicationsUseCase
    {
        Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model);
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

        public async Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model)
        {
            var response = await _applicationService.GetApplications(model);

            if (response == null || !response.Data.Any())
            {
                return null;
            }

            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.Application, string.Empty);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }

            return response;
        }
    }
}