using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;

namespace CheckYourEligibility.API.UseCases
{
    public interface IGetApplicationUseCase
    {
        Task<ApplicationItemResponse> Execute(string guid);
    }

    public class GetApplicationUseCase : IGetApplicationUseCase
    {
        private readonly IApplication _applicationGateway;
        private readonly IAudit _auditGateway;

        public GetApplicationUseCase(IApplication applicationGateway, IAudit auditGateway)
        {
            _applicationGateway = applicationGateway;
            _auditGateway = auditGateway;
        }

        public async Task<ApplicationItemResponse> Execute(string guid)
        {
            var response = await _applicationGateway.GetApplication(guid);
            if (response == null)
            {
                return null;
            }
            await _auditGateway.CreateAuditEntry(AuditType.Application, guid);

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