// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.NetworkInformation;

namespace CheckYourEligibility.Services
{
    public class SchoolSearchService : ISchoolsSearch
    {
        const int takeScoolResultsMax = 20;

        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        private readonly IEnumerable<School> _schools;

        public SchoolSearchService(ILoggerFactory logger, IEligibilityCheckContext dbContext)
        {
            _logger = logger.CreateLogger("SchoolSearchService");
            _db = Guard.Against.Null(dbContext);
            _schools = _db.Schools.Include(x=>x.LocalAuthority).ToList();
        }

        public async Task<IEnumerable<Domain.Responses.School>?> Search(string query)
        {
            var results = new List<Domain.Responses.School>();
            var queryResult = new List<School>();
            Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(query);
            foreach (var item in _schools)
            {
                int levenshteinDistance = lev.DistanceFrom(item.EstablishmentName);
                if (levenshteinDistance < 20)
                {
                    item.LevenshteinDistance = levenshteinDistance;
                    queryResult.Add(item);
                }
            }

            if (queryResult.Any())
            {
                return queryResult
                    .OrderBy(x => x.LevenshteinDistance)
                    .ThenBy(x => x.EstablishmentName).Take(takeScoolResultsMax)
                    .Select(x => new Domain.Responses.School()
                    {
                        Id = x.Urn,
                        Name = x.EstablishmentName,
                        Postcode = x.Postcode,
                        Locality = x.Locality,
                        County = x.County,
                        Street = x.Street,
                        Town = x.Town,
                        La = x.LocalAuthority.LaName
                    });
            }
            return null;
        }
    }
}

