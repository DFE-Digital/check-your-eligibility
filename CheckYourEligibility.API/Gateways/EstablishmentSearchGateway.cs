using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Gateways.Interfaces;
using F23.StringSimilarity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.API.Gateways
{
    public class EstablishmentSearchGateway : IEstablishmentSearch
    {
        const int takeScoolResultsMax = 20;

        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;

        public EstablishmentSearchGateway(ILoggerFactory logger, IEligibilityCheckContext dbContext)
        {
            _logger = logger.CreateLogger("EstablishmentSearchService");
            _db = dbContext;
        }

        [ExcludeFromCodeCoverage(Justification = "memory only db breaks test in full run, works fine run locally")]
          public async Task<IEnumerable<API.Boundary.Responses.Establishment>?> Search(string query)
        {
            var results = new List<API.Boundary.Responses.Establishment>();

            
            if (int.TryParse(query, out var EstablishmentId))
            {

                var establishmentFromUrn = _db.Establishments
                    .Include(x => x.LocalAuthority)
                    .FirstOrDefault(x => x.StatusOpen && x.EstablishmentId == EstablishmentId);
                
                if (establishmentFromUrn != null)
                {
                    var item = new API.Boundary.Responses.Establishment()
                    {
                        Id = establishmentFromUrn.EstablishmentId,
                        Name = establishmentFromUrn.EstablishmentName,
                        Postcode = establishmentFromUrn.Postcode,
                        Locality = establishmentFromUrn.Locality,
                        County = establishmentFromUrn.County,
                        Street = establishmentFromUrn.Street,
                        Town = establishmentFromUrn.Town,
                        La = establishmentFromUrn.LocalAuthority.LaName,
                        Type =establishmentFromUrn.Type
                    };
                    results.Add( item);
                };
                return results;
            }
            var allEstablishments = _db.Establishments.Where(x => x.StatusOpen
            && x.EstablishmentName.Contains(query))
                .Include(x => x.LocalAuthority);

            var queryResult = new List<Establishment>();
            
            var lev = new NormalizedLevenshtein();
            double levenshteinDistance;
            foreach (var item in allEstablishments)
            {
                levenshteinDistance = lev.Distance(query.ToUpper(), item.EstablishmentName.ToUpper());// lev.DistanceFrom(item.EstablishmentName);
                if (levenshteinDistance < 20)
                {
                    item.LevenshteinDistance = levenshteinDistance;
                    queryResult.Add(item);
                }
            }


            return queryResult.Where(x => x.EstablishmentName.ToUpper().Contains(query.ToUpper()))
                .OrderBy(x => x.LevenshteinDistance)
                .ThenBy(x => x.EstablishmentName).Take(takeScoolResultsMax)
                .Select(x => new API.Boundary.Responses.Establishment()
                {
                    Id = x.EstablishmentId,
                    Name = x.EstablishmentName,
                    Postcode = x.Postcode,
                    Locality = x.Locality,
                    County = x.County,
                    Street = x.Street,
                    Town = x.Town,
                    La = x.LocalAuthority.LaName,
                    Distance = x.LevenshteinDistance,
                    Type = x.Type,
                });

        }
    }
}

