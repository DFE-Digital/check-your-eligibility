// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Core;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using System.Globalization;

namespace CheckYourEligibility.Services
{
    public partial class ApplicationService : BaseService, IApplication
    {
        const int referenceMaxValue = 99999999;
        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        private static Random randomNumber;
        private readonly int _hashCheckDays;

        public ApplicationService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, IConfiguration configuration) : base()
        {
            _logger = logger.CreateLogger("ServiceCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);

            randomNumber ??= new Random(referenceMaxValue);
            _hashCheckDays = configuration.GetValue<short>("HashCheckDays");
        }

        public async Task<ApplicationResponse> PostApplication(ApplicationRequestData data)
        {
            try
            {
                var item = _mapper.Map<Application>(data);
                var hashCheck = GetHash(data.Type, item);
                if (hashCheck == null)
                {
                    throw new Exception($"No Check found. Type:- {data.Type}");
                }
                item.ApplicationID = Guid.NewGuid().ToString();
                item.Type = hashCheck.Type;
                item.Reference = GetReference();
                item.EligibilityCheckHashID = hashCheck?.EligibilityCheckHashID;
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;

                if (hashCheck.Outcome == CheckEligibilityStatus.eligible)
                {
                    item.Status = Domain.Enums.ApplicationStatus.Entitled;
                }
                else
                {
                    item.Status = Domain.Enums.ApplicationStatus.EvidenceNeeded;
                }

                try
                {
                    var establishment = _db.Establishments
                   .Include(x => x.LocalAuthority)
                   .First(x => x.EstablishmentId == data.Establishment);
                    item.LocalAuthorityId = establishment.LocalAuthorityId;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unable to find school:- {data.Establishment}, {ex.Message}");
                }


                await _db.Applications.AddAsync(item);
                await AddStatusHistory(item, Domain.Enums.ApplicationStatus.Entitled);

                await _db.SaveChangesAsync();

                var saved = _db.Applications
                    .First(x => x.ApplicationID == item.ApplicationID);

                TrackMetric($"Application {item.Type}", 1);

                return await GetApplication(saved.ApplicationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post application");
                throw;
            }
        }


        public async Task<ApplicationResponse?> GetApplication(string guid)
        {
            var result = await _db.Applications
                .Include(x => x.Statuses)
                .Include(x => x.Establishment)
                .ThenInclude(x => x.LocalAuthority)
                .Include(x => x.User)
                .Include(x => x.EligibilityCheckHash)
                .FirstOrDefaultAsync(x => x.ApplicationID == guid);
            if (result != null)
            {
                var item = _mapper.Map<ApplicationResponse>(result);
                item.CheckOutcome = new ApplicationResponse.ApplicationHash { Outcome = result.EligibilityCheckHash?.Outcome.ToString() };
                return item;
            }

            return null;
        }

        public async Task<ApplicationSearchResponse> GetApplications(ApplicationRequestSearch model)
        {
            IEnumerable<Application> results = new List<Application>();
            if (model.Data.Statuses != null)
                foreach (var status in model.Data.Statuses)
                {
                    var resultsForStatus = GetSearchResults(model, status);
                    results = results.Union(resultsForStatus);
                }
            else
            {
                results = GetSearchResults(model, null);
            }

            // Calculate total records and total pages
            int totalRecords = results.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / model.PageSize);

            // Pagination logic
            model.PageNumber = model.PageNumber <= 0 ? 1 : model.PageNumber;
            var pagedResults = results
                .Skip((model.PageNumber - 1) * model.PageSize)
                .Take(model.PageSize);

            // Use AutoMapper or manual mapping to map Application entities to ApplicationResponse DTOs
            var mappedResults = _mapper.Map<IEnumerable<ApplicationResponse>>(pagedResults);

            // Return paginated and mapped results
            return new ApplicationSearchResponse { Data = mappedResults, TotalRecords = totalRecords, TotalPages = totalPages };
        }

        private IEnumerable<Application> GetSearchResults(ApplicationRequestSearch model, Domain.Enums.ApplicationStatus? status)
        {
            IQueryable<Application> results = results = _db.Applications
               .Include(x => x.Statuses)
               .Include(x => x.Establishment)
               .ThenInclude(x => x.LocalAuthority)
               .Include(x => x.User)
               .Where(x => x.Type == model.Data.Type);
            if (model.Data?.Establishment != null)
                results = results.Where(x => x.EstablishmentId == model.Data.Establishment);
            if (model.Data?.LocalAuthority != null)
                results = results.Where(x => x.LocalAuthorityId == model.Data.LocalAuthority);
            if (status != null)
                results = results.Where(x => x.Status == status);

            if (!string.IsNullOrEmpty(model.Data?.ParentNationalInsuranceNumber))
                results = results.Where(x => x.ParentNationalInsuranceNumber == model.Data.ParentNationalInsuranceNumber);
            if (!string.IsNullOrEmpty(model.Data?.ParentLastName))
                results = results.Where(x => x.ParentLastName == model.Data.ParentLastName);
            if (!string.IsNullOrEmpty(model.Data?.ParentNationalAsylumSeekerServiceNumber))
                results = results.Where(x => x.ParentNationalAsylumSeekerServiceNumber == model.Data.ParentNationalAsylumSeekerServiceNumber);
            if (!string.IsNullOrEmpty(model.Data?.ParentDateOfBirth))
                results = results.Where(x => x.ParentDateOfBirth == DateTime.ParseExact(model.Data.ParentDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(model.Data?.ChildLastName))
                results = results.Where(x => x.ChildLastName == model.Data.ChildLastName);
            if (!string.IsNullOrEmpty(model.Data?.ChildDateOfBirth))
                results = results.Where(x => x.ChildDateOfBirth == DateTime.ParseExact(model.Data.ChildDateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(model.Data?.Reference))
                results = results.Where(x => x.Reference == model.Data.Reference);
            if (model.Data?.DateRange != null)
                results = results.Where(x => x.Created > model.Data.DateRange.DateFrom && x.Created < model.Data.DateRange.DateTo);
            if (!string.IsNullOrEmpty(model.Data?.Keyword))
                results = results.Where(
                    x =>
                        x.Reference.Contains(model.Data.Keyword) ||
                        x.ChildFirstName.Contains(model.Data.Keyword) ||
                        x.ChildLastName.Contains(model.Data.Keyword) ||
                        x.ParentFirstName.Contains(model.Data.Keyword) ||
                        x.ParentLastName.Contains(model.Data.Keyword) ||
                        x.ParentNationalInsuranceNumber.Contains(model.Data.Keyword) ||
                        x.ParentNationalAsylumSeekerServiceNumber.Contains(model.Data.Keyword) ||
                        x.ParentEmail.Contains(model.Data.Keyword)
                );
            return results.OrderBy(x => x.Created).ToList();
        }

        public async Task<ApplicationStatusUpdateResponse> UpdateApplicationStatus(string guid, ApplicationStatusData data)
        {
            var result = await _db.Applications.FirstOrDefaultAsync(x => x.ApplicationID == guid);
            if (result != null)
            {
                result.Status = data.Status;
                await AddStatusHistory(result, result.Status.Value);

                result.Updated = DateTime.UtcNow;
                var updates = await _db.SaveChangesAsync();
                TrackMetric($"Application Status Change {result.Status}", 1);
                TrackMetric($"Application Status Change Establishment:-{result.EstablishmentId} {result.Status}", 1);
                TrackMetric($"Application Status Change La:-{result.LocalAuthorityId} {result.Status}", 1);
                return new ApplicationStatusUpdateResponse { Data = new ApplicationStatusDataResponse { Status = result.Status.Value.ToString() } };
            }

            return null;
        }

        #region Private

        private string GetReference()
        {
            var unique = false;
            string nextReference = string.Empty;
            while (!unique)
            {
                nextReference = randomNumber.Next(1, referenceMaxValue).ToString();
                unique = _db.Applications.FirstOrDefault(x => x.Reference == nextReference) == null;
            }

            return nextReference;
        }


        private EligibilityCheckHash? GetHash(CheckEligibilityType type, Application data)
        {
            var age = DateTime.UtcNow.AddDays(-_hashCheckDays);
            var hash = CheckEligibilityService.GetHash(new CheckProcessData
            {
                DateOfBirth = data.ParentDateOfBirth.ToString("yyyy-MM-dd"),
                NationalInsuranceNumber = data.ParentNationalAsylumSeekerServiceNumber,
                NationalAsylumSeekerServiceNumber = data.ParentNationalInsuranceNumber,
                LastName = data.ParentLastName.ToUpper(),
                Type = type
            });
            return _db.EligibilityCheckHashes.FirstOrDefault(x => x.Hash == hash && x.TimeStamp >= age);
        }

        private async Task AddStatusHistory(Application application, Domain.Enums.ApplicationStatus applicationStatus)
        {
            var status = new Data.Models.ApplicationStatus()
            {
                ApplicationStatusID = Guid.NewGuid().ToString(),
                ApplicationID = application.ApplicationID,
                Type = applicationStatus,
                TimeStamp = DateTime.UtcNow
            };
            await _db.ApplicationStatuses.AddAsync(status);
        }

        #endregion
    }


}
