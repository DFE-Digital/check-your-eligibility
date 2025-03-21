using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface ISearchEstablishmentsUseCase
    {
        Task<IEnumerable<Establishment>> Execute(string query);
    }

    public class SearchEstablishmentsUseCase : ISearchEstablishmentsUseCase
    {
        private readonly IEstablishmentSearch _service;
        private readonly IAudit _auditService;

        public SearchEstablishmentsUseCase(IEstablishmentSearch service, IAudit auditService)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<IEnumerable<Establishment>> Execute(string query)
        {
            Guard.Against.NullOrWhiteSpace(query, nameof(query));
            Guard.Against.OutOfRange(query.Length, nameof(query.Length), 3, int.MaxValue);

            var results = await _service.Search(query);
            await _auditService.CreateAuditEntry(AuditType.Establishment, string.Empty);
            return results ?? Enumerable.Empty<Establishment>();
        }
    }
}