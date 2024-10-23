using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services.Interfaces;
using F23.StringSimilarity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Services
{
    public class SchoolSearchService : ISchoolsSearch
    {
        const int takeScoolResultsMax = 20;

        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;

        public SchoolSearchService(ILoggerFactory logger, IEligibilityCheckContext dbContext)
        {
            _logger = logger.CreateLogger("SchoolSearchService");
            _db = Guard.Against.Null(dbContext);
        }

        [ExcludeFromCodeCoverage(Justification = "memory only db breaks test in full run, works fine run locally")]
          public async Task<IEnumerable<Domain.Responses.School>?> Search(string query)
        {
            var results = new List<Domain.Responses.School>();

            
            if (int.TryParse(query, out var SchoolId))
            {

                var schoolFromUrn = _db.Schools
                    .Include(x => x.LocalAuthority)
                    .FirstOrDefault(x => x.StatusOpen && x.SchoolId == SchoolId);
                
                if (schoolFromUrn != null)
                {
                    var item = new Domain.Responses.School()
                    {
                        Id = schoolFromUrn.SchoolId,
                        Name = schoolFromUrn.EstablishmentName,
                        Postcode = schoolFromUrn.Postcode,
                        Locality = schoolFromUrn.Locality,
                        County = schoolFromUrn.County,
                        Street = schoolFromUrn.Street,
                        Town = schoolFromUrn.Town,
                        La = schoolFromUrn.LocalAuthority.LaName
                    };
                    results.Add( item);
                };
                return results;
            }
            var allSchools = _db.Schools.Where(x => x.StatusOpen
            && x.EstablishmentName.Contains(query))
                .Include(x => x.LocalAuthority);

            var queryResult = new List<School>();
            
            var lev = new NormalizedLevenshtein();
            double levenshteinDistance;
            foreach (var item in allSchools)
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
                .Select(x => new Domain.Responses.School()
                {
                    Id = x.SchoolId,
                    Name = x.EstablishmentName,
                    Postcode = x.Postcode,
                    Locality = x.Locality,
                    County = x.County,
                    Street = x.Street,
                    Town = x.Town,
                    La = x.LocalAuthority.LaName,
                    Distance = x.LevenshteinDistance
                });

        }
    }
}

