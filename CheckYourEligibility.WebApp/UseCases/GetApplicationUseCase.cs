using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface IGetApplicationUseCase
    {
        Task<ApplicationItemResponse> Execute(string guid);
    }

    public class GetApplicationUseCase : IGetApplicationUseCase
    {
        private readonly IApplication _applicationService;
        private readonly IAudit _auditService;

        public GetApplicationUseCase(IApplication applicationService, IAudit auditService)
        {
            _applicationService = Guard.Against.Null(applicationService);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<ApplicationItemResponse> Execute(string guid)
        {
            var response = await _applicationService.GetApplication(guid);
            if (response == null)
            {
                return null;
            }
            await _auditService.CreateAuditEntry(AuditType.Application, guid);

            return new ApplicationItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{response.Id}"
                }
            };
        }
    }
}