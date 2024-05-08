// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.Services
{
    public partial class FsmCheckEligibilityService : BaseService, IFsmCheckEligibility
    {
        const int referenceMaxValue = 99999999;
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        private readonly QueueClient _queueClient;
        private const int SurnameCheckCharachters = 3;
        private static Random randomNumber;
        private readonly IDwpService _dwpService;
        private readonly int _hashCheckDays;

        public FsmCheckEligibilityService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, QueueServiceClient queueClientService,
            IConfiguration configuration, IDwpService dwpService) : base()
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            _dwpService = Guard.Against.Null(dwpService);
            var queName = configuration.GetValue<string>("QueueFsmCheckStandard");
            if (queName != "notSet")
            {
                _queueClient = queueClientService.GetQueueClient(queName);
            }
            if (randomNumber == null)
            {
               randomNumber = new Random(referenceMaxValue);
            }
            _hashCheckDays = configuration.GetValue<short>("HashCheckDays");
        }

        public async Task<PostCheckResult> PostCheck(CheckEligibilityRequestDataFsm data)
        {
            var item = _mapper.Map<EligibilityCheck>(data);
            try
            {
                item.EligibilityCheckID = Guid.NewGuid().ToString();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;

                item.Status = CheckEligibilityStatus.queuedForProcessing;
                item.Type = CheckEligibilityType.FreeSchoolMeals;
                var checkHashResult = CheckHashResult(item);
                if (checkHashResult != null)
                {
                    item.Status = checkHashResult.Outcome;
                    item.EligibilityCheckHashID = checkHashResult.EligibilityCheckHashID;
                    item.EligibilityCheckHash = checkHashResult;
                }
                await _db.CheckEligibilities.AddAsync(item);
                await _db.SaveChangesAsync();
                if (checkHashResult == null)
                {
                    if (_queueClient != null)
                    {
                        await _queueClient.SendMessageAsync(
                            JsonConvert.SerializeObject(new QueueMessageCheck() { Type = item.Type.ToString(), Guid = item.EligibilityCheckID, Url = $"{FSMLinks.ProcessLink}{item.EligibilityCheckID}" }));
                    }
                }

                return new PostCheckResult { Id = item.EligibilityCheckID, Status = item.Status };
            }
            catch (Exception ex)
            {
                LogApiEvent(this.GetType().Name, data, item);
                _logger.LogError(ex, "Db post");
                throw;
            }
        }
        private EligibilityCheckHash? CheckHashResult(EligibilityCheck item)
        {
            var age = DateTime.UtcNow.AddDays(-_hashCheckDays);
            var hash = GetHash(item);
            return  _db.EligibilityCheckHashes.FirstOrDefault(x => x.Hash == hash && x.TimeStamp >= age);
        }

        public async Task<CheckEligibilityStatus?> GetStatus(string guid)
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x=> x.EligibilityCheckID == guid);
            if (result != null)
            {
               // var x = await _dwpService.GetStatus(guid);
                return result.Status;
            }
            return null;
        }

        public async Task<CheckEligibilityStatus?> ProcessCheck(string guid)
        {         
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                if (result.Status != CheckEligibilityStatus.queuedForProcessing)
                {
                    LogApiEvent(this.GetType().Name, guid, "CheckItem not queuedForProcessing.");
                    throw new ProcessCheckException("CheckItem not queuedForProcessing.");
                }
                var source = ProcessEligibilityCheckSource.HMRC;
                CheckEligibilityStatus checkResult = CheckEligibilityStatus.parentNotFound;
                if (!result.NINumber.IsNullOrEmpty())
                {
                    checkResult = await HMRC_Check(result);
                    if (checkResult == CheckEligibilityStatus.parentNotFound)
                    {
                        checkResult = await DWP_Check(result);
                        source = ProcessEligibilityCheckSource.DWP;
                    }
                }
                else if (!result.NASSNumber.IsNullOrEmpty())
                {
                    checkResult = await HO_Check(result);
                    source = ProcessEligibilityCheckSource.HO;
                }
                result.Status = checkResult;
                result.Updated = DateTime.UtcNow;
               
                if (checkResult != CheckEligibilityStatus.DwpError)
                {
                    HashCheckResult(result, checkResult, source);
                }
                var updates = await _db.SaveChangesAsync();
                return result.Status;
            }
            else
            {
                LogApiEvent(this.GetType().Name, guid, "failed to find checkItem.");
            }
            return null;
        }

        public async Task<CheckEligibilityItemFsm?> GetItem(string guid)
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                var item = _mapper.Map<CheckEligibilityItemFsm>(result);
                return  item ;
            }
            return null;
        }

        public async Task<ApplicationSave> PostApplication(ApplicationRequestData data)
        {
            try
            {
                var item = _mapper.Map<Application>(data);
                item.ApplicationID = Guid.NewGuid().ToString();
                item.Reference = GetReference();
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
               .Where(x => x.Type == CheckEligibilityType.ApplcicationFsm);

            if (model.School != null) {
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
                return new ApplicationStatusUpdateResponse { Data = new ApplicationStatusDataResponse { Status = result.Status.Value.ToString()} };
            }

            return null;
        }

        public string GetHash(EligibilityCheck item)
        {
            var key  = string.IsNullOrEmpty(item.NINumber) ? item.NASSNumber : item.NINumber;
            var input = $"{item.LastName}{key}{item.DateOfBirth.ToString("d")}{item.Type}";
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }

        #region Private

        private async void HashCheckResult(EligibilityCheck item, CheckEligibilityStatus checkResult, ProcessEligibilityCheckSource source)
        {
            var hash = GetHash(item);
            var HashItem = new EligibilityCheckHash()
            {
                EligibilityCheckHashID = Guid.NewGuid().ToString(),
                Hash = hash,
                Type = item.Type,
                Outcome = checkResult,
                TimeStamp = DateTime.UtcNow,
                Source = source
            };
            item.EligibilityCheckHashID = HashItem.EligibilityCheckHashID;
            await _db.EligibilityCheckHashes.AddAsync(HashItem);
        }


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

        private async Task<CheckEligibilityStatus> HO_Check(EligibilityCheck data)
        {
            var checkReults = _db.FreeSchoolMealsHO.Where(x =>
           x.NASS == data.NASSNumber
           && x.DateOfBirth == data.DateOfBirth).Select(x => x.LastName);
            return CheckSurname(data.LastName, checkReults);
        }

        private async Task<CheckEligibilityStatus> HMRC_Check(EligibilityCheck data)
        {
            var checkReults = _db.FreeSchoolMealsHMRC.Where(x =>
            x.FreeSchoolMealsHMRCID == data.NINumber
            && x.DateOfBirth == data.DateOfBirth).Select(x => x.Surname);

            return CheckSurname(data.LastName, checkReults);
        }

        private async Task<CheckEligibilityStatus> DWP_Check(EligibilityCheck data)
        {
            var checkResult = CheckEligibilityStatus.parentNotFound;

            var citizenRequest = new CitizenMatchRequest
            {
                Jsonapi = new CitizenMatchRequest.CitizenMatchRequest_Jsonapi { Version = "2.0" },
                Data = new CitizenMatchRequest.CitizenMatchRequest_Data
                {
                    Type = "Match",
                    Attributes = new CitizenMatchRequest.CitizenMatchRequest_Attributes
                    {
                        LastName = data.LastName,
                        NinoFragment = data.NINumber,
                        DateOfBirth = data.DateOfBirth.ToString("yyyy-MM-dd")
                    }
                }
            };


            //check citizen
            // if a guid is not valid ie the request failed then the status is updated
            var guid = await _dwpService.GetCitizen(citizenRequest);
            if (!Guid.TryParse(guid, out _))
            {
                return (CheckEligibilityStatus)Enum.Parse(typeof(CheckEligibilityStatus), guid);
            }
            
            if (!string.IsNullOrEmpty(guid))
            {
                //check for benefit
                var result = await _dwpService.CheckForBenefit(guid);
                if (result.StatusCode == StatusCodes.Status200OK)
                {
                    checkResult = CheckEligibilityStatus.eligible;
                }
                else if(result.StatusCode == StatusCodes.Status404NotFound)
                {
                    checkResult = CheckEligibilityStatus.notEligible;
                }
                else
                {
                    _logger.LogError($"DwpError unknown Response status code:-{result.StatusCode}.");
                    checkResult = CheckEligibilityStatus.DwpError;
                }
            }

            return checkResult;
        }

        private CheckEligibilityStatus CheckSurname(string lastNamePartial, IQueryable<string> validData)
        {
            if (validData.Any())
            {
                return validData.FirstOrDefault(x => x.ToUpper().StartsWith(lastNamePartial.Substring(0, SurnameCheckCharachters).ToUpper())) != null
                    ? CheckEligibilityStatus.eligible : CheckEligibilityStatus.parentNotFound;
            };
            return CheckEligibilityStatus.parentNotFound;
        }

        #endregion
    }
}
