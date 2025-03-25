using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.Interfaces;

namespace CheckYourEligibility.API.UseCases;

public interface ISearchApplicationsUseCase
{
    Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model, string? localAuthorityId = null);
}

public class SearchApplicationsUseCase : ISearchApplicationsUseCase
{
    private readonly IApplication _applicationGateway;
    private readonly IAudit _auditGateway;

    public SearchApplicationsUseCase(IApplication applicationGateway, IAudit auditGateway)
    {
        _applicationGateway = applicationGateway;
        _auditGateway = auditGateway;
    }

    public async Task<ApplicationSearchResponse> Execute(ApplicationRequestSearch model,
        string? localAuthorityId = null)
    {
        var response = await _applicationGateway.GetApplications(model);

        if (response == null || !response.Data.Any()) return null;
        await _auditGateway.CreateAuditEntry(AuditType.Administration, string.Empty);

        return response;
    }
}