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
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.Services
{
    public partial class FsmCheckEligibilityService : BaseService, IFsmCheckEligibility
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        protected readonly IAudit _audit;
        private  QueueClient _queueClientStandard;
        private QueueClient _queueClientBulk;
        private const int SurnameCheckCharachters = 3;
     
        private readonly IDwpService _dwpService;
        private readonly IHash _hashService;

        public FsmCheckEligibilityService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, QueueServiceClient queueClientService,
            IConfiguration configuration, IDwpService dwpService, IAudit audit, IHash hashService) : base()
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            _dwpService = Guard.Against.Null(dwpService);
            _audit = Guard.Against.Null(audit);
            _hashService = Guard.Against.Null(hashService);

            setQueueStandard(configuration.GetValue<string>("QueueFsmCheckStandard"), queueClientService);
            setQueueBulk(configuration.GetValue<string>("QueueFsmCheckBulk"), queueClientService);
        }

        [ExcludeFromCodeCoverage]
        private void setQueueStandard(string queName, QueueServiceClient queueClientService)
        {
            if (queName != "notSet")
            {
                _queueClientStandard = queueClientService.GetQueueClient(queName);
            }
        }

        [ExcludeFromCodeCoverage]
        private void setQueueBulk(string queName, QueueServiceClient queueClientService)
        {
            if (queName != "notSet")
            {
                _queueClientBulk = queueClientService.GetQueueClient(queName);
            }
        }

        public async Task PostCheck(IEnumerable<CheckEligibilityRequestDataFsm> data, string groupId)
        {
            
            foreach (var item in data)
            {
                await PostCheck(item, groupId);
            }
        }

        public async Task<PostCheckResult> PostCheck(CheckEligibilityRequestDataFsm data, string? group = null)
        {
            var item = _mapper.Map<EligibilityCheck>(data);
            try
            {
                item.Group = group;
                item.EligibilityCheckID = Guid.NewGuid().ToString();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;

                item.Status = CheckEligibilityStatus.queuedForProcessing;
                item.Type = CheckEligibilityType.FreeSchoolMeals;
                var checkHashResult = await  _hashService.Exists(item);
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
                   await SendMessage(item);
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

        [ExcludeFromCodeCoverage(Justification = "Queue is external dependency.")]
        private async Task SendMessage(EligibilityCheck item)
        {
            if (_queueClientStandard != null)
            {
                if (item.Group.IsNullOrEmpty())
                {
                    await _queueClientStandard.SendMessageAsync(
                                    JsonConvert.SerializeObject(new QueueMessageCheck()
                                    {
                                        Type = item.Type.ToString(),
                                        Guid = item.EligibilityCheckID,
                                        ProcessUrl = $"{FSMLinks.ProcessLink}{item.EligibilityCheckID}",
                                        SetStatusUrl = $"{FSMLinks.GetLink}{item.EligibilityCheckID}/status"
                                    }));
                }
                else
                {
                    await _queueClientBulk.SendMessageAsync(
                                JsonConvert.SerializeObject(new QueueMessageCheck()
                                {
                                    Type = item.Type.ToString(),
                                    Guid = item.EligibilityCheckID,
                                    ProcessUrl = $"{FSMLinks.ProcessLink}{item.EligibilityCheckID}",
                                    SetStatusUrl = $"{FSMLinks.GetLink}{item.EligibilityCheckID}/status"
                                }));
                }
            }
        }
        

        public async Task<CheckEligibilityStatus?> GetStatus(string guid)
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x=> x.EligibilityCheckID == guid);
            if (result != null)
            {
                return result.Status;
            }
            return null;
        }

        public async Task<CheckEligibilityStatus?> ProcessCheck(string guid, AuditData auditDataTemplate)
        {         
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                if (result.Status != CheckEligibilityStatus.queuedForProcessing)
                {
                    LogApiEvent(this.GetType().Name, guid, $"CheckItem not queuedForProcessing. {GetCurrentMethod()}");
                    throw new ProcessCheckException($"Error checkItem {guid} not queuedForProcessing.");
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
               
                if (checkResult == CheckEligibilityStatus.DwpError)
                {
                    // Revert status back and do not save changes
                    result.Status = CheckEligibilityStatus.queuedForProcessing;
                    LogApiEvent(this.GetType().Name, guid, "Dwp Error", "There has been an error calling DWP");
                }
                else
                {
                    await _hashService.Create(result, checkResult, source, auditDataTemplate);
                   
                    var updates = await _db.SaveChangesAsync();
                }
                
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

        public async Task<IEnumerable<CheckEligibilityItemFsm>> GetBulkCheckResults(string guid)
        {
            var resultList =  _db.CheckEligibilities
                .Where(x => x.Group == guid)
                .OrderBy(x=>x.Sequence);
            if (resultList != null && resultList.Any())
            {
                var items = _mapper.Map<List<CheckEligibilityItemFsm>>(resultList);
                return items;
            }
            return null;
        }

        public  static string GetHash(EligibilityCheck item)
        {
            var key  = string.IsNullOrEmpty(item.NINumber) ? item.NASSNumber : item.NINumber;
            var input = $"{item.LastName}{key}{item.DateOfBirth.ToString("d")}{item.Type}";
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }

        public async Task<CheckEligibilityStatusResponse> UpdateEligibilityCheckStatus(string guid, EligibilityCheckStatusData data)
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                result.Status = data.Status;
                result.Updated = DateTime.UtcNow;
                var updates = await _db.SaveChangesAsync();
                return new CheckEligibilityStatusResponse { Data = new StatusValue { Status = result.Status.ToString() } };
            }

            return null;
        }

        public async Task<BulkStatus?> GetBulkStatus(string guid)
        {
            var results = _db.CheckEligibilities
                .Where(x => x.Group == guid)
                .GroupBy(n=> n.Status)
                .Select(n=> new {Status = n.Key, ct = n.Count()});
            if (results.Any())
            {
                return new BulkStatus {Total = results.Sum(s => s.ct), Complete = results.Where(a => a.Status != CheckEligibilityStatus.queuedForProcessing).Sum(s => s.ct) };
            }
            return null;
        }

        #region Private

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
