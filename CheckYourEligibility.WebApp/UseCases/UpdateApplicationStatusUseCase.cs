using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface IUpdateApplicationStatusUseCase
    {
        Task<ApplicationStatusUpdateResponse> Execute(string guid, ApplicationStatusUpdateRequest model);
    }

    public class UpdateApplicationStatusUseCase : IUpdateApplicationStatusUseCase
    {
        private readonly IApplication _applicationService;
        private readonly IAudit _auditService;

        public UpdateApplicationStatusUseCase(IApplication applicationService, IAudit auditService)
        {
            _applicationService = Guard.Against.Null(applicationService);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<ApplicationStatusUpdateResponse> Execute(string guid, ApplicationStatusUpdateRequest model)
        {
            var response = await _applicationService.UpdateApplicationStatus(guid, model.Data);
            if (response == null)
            {
                return null;
            }

            await _auditService.CreateAuditEntry(AuditType.Application, guid);
            
            return new ApplicationStatusUpdateResponse
            {
                Data = response.Data
            };
        }
    }
}