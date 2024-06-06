// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Index.HPRtree;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.Services
{
    public partial class FsmApplicationService : BaseService, IFsmApplication
    {
        const int referenceMaxValue = 99999999;
        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        private static Random randomNumber;
        private readonly int _hashCheckDays;

        public FsmApplicationService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, IConfiguration configuration) : base()
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);

            randomNumber ??= new Random(referenceMaxValue);
            _hashCheckDays = configuration.GetValue<short>("HashCheckDays");
        }

        public async Task<ApplicationSave> PostApplication(ApplicationRequestData data)
        {
            try
            {
                var item = _mapper.Map<Application>(data);
                item.ApplicationID = Guid.NewGuid().ToString();
                item.Reference = GetReference();
                item.EligibilityCheckHashID = GetHash(item); 
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;
                item.Type = CheckEligibilityType.ApplcicationFsm;
                item.Status = Domain.Enums.ApplicationStatus.Open;

                var school = _db.Schools
                    .Include(x => x.LocalAuthority)
                    .First(x => x.SchoolId == data.School);
                item.LocalAuthorityId = school.LocalAuthorityId;

                await _db.Applications.AddAsync(item);
                await AddStatusHistory(item, Domain.Enums.ApplicationStatus.Open);

                await _db.SaveChangesAsync();

                var saved = _db.Applications
                    .First(x => x.ApplicationID == item.ApplicationID);

                var returnItem = _mapper.Map<ApplicationSave>(item);

                return returnItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post application");
                throw;
            }
        }

        private string? GetHash(Application data)
        {
            var age = DateTime.UtcNow.AddDays(-_hashCheckDays);
            var hash = FsmCheckEligibilityService.GetHash(new EligibilityCheck
            {
                DateOfBirth = data.ParentDateOfBirth,
                NASSNumber = data.ParentNationalAsylumSeekerServiceNumber,
                NINumber = data.ParentNationalInsuranceNumber,
                LastName = data.ParentLastName,
                Type = CheckEligibilityType.FreeSchoolMeals
            });
            var hashItem = _db.EligibilityCheckHashes.FirstOrDefault(x => x.Hash == hash && x.TimeStamp >= age);
            if (hashItem != null)
            {
                return hashItem.EligibilityCheckHashID;
            }
            return null;
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

        public async Task<ApplicationResponse?> GetApplication(string guid)
        {
            var result = await _db.Applications
                .Include(x => x.Statuses)
                .Include(x => x.School)
                .ThenInclude(x => x.LocalAuthority)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ApplicationID == guid);
            if (result != null)
            {
                var item = _mapper.Map<ApplicationResponse>(result);
                return item;
            }

            return null;
        }

        public async Task<IEnumerable<ApplicationResponse>> GetApplications(ApplicationRequestSearchData model)
        {
            var results = _db.Applications
               .Include(x => x.Statuses)
               .Include(x => x.School)
               .ThenInclude(x => x.LocalAuthority)
               .Include(x => x.User)
               .Where(x => x.Type == CheckEligibilityType.ApplcicationFsm);

            if (model.School != null)
            {
                results = results.Where(x => x.SchoolId == model.School);
            }
            if (model.localAuthority != null)
            {
                results = results.Where(x => x.LocalAuthorityId == model.localAuthority);
            }
            if (model.Status != null)
            {
                results = results.Where(x => x.Status == model.Status);
            }

            return _mapper.Map<List<ApplicationResponse>>(results);
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
       
        #endregion
    }


}
