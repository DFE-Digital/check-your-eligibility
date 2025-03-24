using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace CheckYourEligibility.API.UseCases
{
    public interface ISearchEstablishmentsUseCase
    {
        Task<IEnumerable<Establishment>> Execute(string query);
    }

    public class SearchEstablishmentsUseCase : ISearchEstablishmentsUseCase
    {
        private readonly IEstablishmentSearch _gateway;
        private readonly IAudit _auditGateway;

        public SearchEstablishmentsUseCase(IEstablishmentSearch Gateway, IAudit auditGateway)
        {
            _gateway = Gateway;
            _auditGateway = auditGateway;
        }

        public async Task<IEnumerable<Establishment>> Execute(string query)
        {
            if(query.IsNullOrEmpty()) throw new ArgumentException();
            if(query.Length<3||query.Length>int.MaxValue) throw new ArgumentException();

            var results = await _gateway.Search(query);
            await _auditGateway.CreateAuditEntry(AuditType.Establishment, string.Empty);
            return results ?? Enumerable.Empty<Establishment>();
        }
    }
}