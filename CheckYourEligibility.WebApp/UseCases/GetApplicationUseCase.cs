using Ardalis.GuardClauses;
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

            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.Application, guid);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }

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