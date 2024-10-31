// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.Services
{
    public partial class CheckEligibilityService : BaseService, ICheckEligibility
    {
        private QueueClient _queueClientStandard;
        private QueueClient _queueClientBulk;
        private const int SurnameCheckCharachters = 3;
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        protected readonly IAudit _audit;
            
        private readonly IDwpService _dwpService;
        private readonly IHash _hashService;
        private string _groupId;

        public CheckEligibilityService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, QueueServiceClient queueClientService,
            IConfiguration configuration, IDwpService dwpService, IAudit audit, IHash hashService) : base()
        {
            _logger = logger.CreateLogger("ServiceCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            _dwpService = Guard.Against.Null(dwpService);
            _audit = Guard.Against.Null(audit);
            _hashService = Guard.Against.Null(hashService);

            setQueueStandard(configuration.GetValue<string>("QueueFsmCheckStandard"), queueClientService);
            setQueueBulk(configuration.GetValue<string>("QueueFsmCheckBulk"), queueClientService);
        }

        public async Task PostCheck<T>(T data, string groupId) where T : IEnumerable<IEligibilityServiceType> 
        {
            _groupId = groupId;
            foreach (var item in data)
            {
                await PostCheck(item);
            }
        }

        public async Task<PostCheckResult> PostCheck<T>(T data) where T : IEligibilityServiceType
        {
            var item = _mapper.Map<EligibilityCheck>(data);

            try
            {
                var baseType = data as CheckEligibilityRequestDataBase;
                item.CheckData = JsonConvert.SerializeObject(data);
                
                item.Type = baseType.Type;

                item.Group = _groupId;
                item.EligibilityCheckID = Guid.NewGuid().ToString();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;

                item.Status = CheckEligibilityStatus.queuedForProcessing;
                
                var checkHashResult = await  _hashService.Exists(JsonConvert.DeserializeObject<CheckProcessData>(item.CheckData));
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
                var checkData = GetCheckProcessData(result.Type,result.CheckData);
                if (result.Status != CheckEligibilityStatus.queuedForProcessing)
                {
                    LogApiEvent(this.GetType().Name, guid, $"CheckItem not queuedForProcessing. {GetCurrentMethod()}");
                    throw new ProcessCheckException($"Error checkItem {guid} not queuedForProcessing. {result.Status}");
                }

                switch (result.Type)
                {
                    case CheckEligibilityType.FreeSchoolMeals:
                        {
                            await Process_StandardCheck(guid, auditDataTemplate, result, checkData);
                        }
                        break;
                    default:
                        break;
                }
                return result.Status;
            }
            else
            {
                LogApiEvent(this.GetType().Name, guid, "failed to find checkItem.");
            }
            return null;
        }


        public async Task<T?> GetItem<T>(string guid) where T : CheckEligibilityItem
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);

            if (result != null)
            {
                var item = _mapper.Map<CheckEligibilityItem>(result);
                var CheckData = GetCheckProcessData(result.Type, result.CheckData);
                item.DateOfBirth = CheckData.DateOfBirth;
                item.NationalInsuranceNumber = CheckData.NationalInsuranceNumber;
                item.NationalAsylumSeekerServiceNumber = CheckData.NationalAsylumSeekerServiceNumber;
                item.LastName = CheckData.LastName;

                return (T)(object)item;

            }
            return default;
        }

        public async Task<T> GetBulkCheckResults<T>(string guid) where T : IList<CheckEligibilityItem>
        {
            var resultList =  _db.CheckEligibilities
                .Where(x => x.Group == guid)
                .OrderBy(x=>x.Sequence);
            if (resultList != null && resultList.Any())
            {
                var type = typeof(T);
                if (type == typeof(IList<CheckEligibilityItem>))
                {
                    var items = _mapper.Map<T>(resultList);
                  
                    return items;
                }
                else
                {
                    throw new Exception($"unable to cast to type {type}");
                }
            }
            return default;
        }

        public  static string GetHash(CheckProcessData item)
        {
            var key  = string.IsNullOrEmpty(item.NationalInsuranceNumber) ? item.NationalAsylumSeekerServiceNumber.ToUpper() : item.NationalInsuranceNumber.ToUpper();
            var input = $"{item.LastName.ToUpper()}{key}{item.DateOfBirth}{item.Type}";
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
                                            ProcessUrl = $"{CheckLinks.ProcessLink}{item.EligibilityCheckID}",
                                            SetStatusUrl = $"{CheckLinks.GetLink}{item.EligibilityCheckID}/status"
                                        }));

                    LogQueueCount(_queueClientStandard);

                }
                else
                {
                    await _queueClientBulk.SendMessageAsync(
                                JsonConvert.SerializeObject(new QueueMessageCheck()
                                {
                                    Type = item.Type.ToString(),
                                    Guid = item.EligibilityCheckID,
                                    ProcessUrl = $"{CheckLinks.ProcessLink}{item.EligibilityCheckID}",
                                    SetStatusUrl = $"{CheckLinks.GetLink}{item.EligibilityCheckID}/status"
                                }));
                    LogQueueCount(_queueClientBulk);
                }
            }
        }


        private async Task Process_StandardCheck(string guid, AuditData auditDataTemplate, EligibilityCheck? result, CheckProcessData checkData)
        {
            var source = ProcessEligibilityCheckSource.HMRC;
            CheckEligibilityStatus checkResult = CheckEligibilityStatus.parentNotFound;
            if (!checkData.NationalInsuranceNumber.IsNullOrEmpty())
            {
                checkResult = await HMRC_Check(checkData);
                if (checkResult == CheckEligibilityStatus.parentNotFound)
                {
                    checkResult = await DWP_Check(checkData);
                    source = ProcessEligibilityCheckSource.DWP;
                }
            }
            else if (!checkData.NationalAsylumSeekerServiceNumber.IsNullOrEmpty())
            {
                checkResult = await HO_Check(checkData);
                source = ProcessEligibilityCheckSource.HO;
            }
            result.Status = checkResult;
            result.Updated = DateTime.UtcNow;

            if (checkResult == CheckEligibilityStatus.DwpError)
            {
                // Revert status back and do not save changes
                result.Status = CheckEligibilityStatus.queuedForProcessing;
                LogApiEvent(this.GetType().Name, guid, "Dwp Error", $"There has been an error calling DWP, Request GUID:-{guid} ");
                TrackMetric($"Dwp Error", 1);
            }
            else
            {
              result.EligibilityCheckHashID =  await _hashService.Create(checkData, checkResult, source, auditDataTemplate);
              await _db.SaveChangesAsync();
            }

            TrackMetric($"FSM Check:-{result.Status}", 1);
            TrackMetric($"FSM Check", 1);
            var processingTime = (DateTime.Now.ToUniversalTime() - result.Created.ToUniversalTime()).Seconds;
            TrackMetric($"Check ProcessingTime (Seconds)", processingTime);
        }

        private CheckProcessData GetCheckProcessData(CheckEligibilityType type, string data)
        {
            switch (type)
            {
                case CheckEligibilityType.FreeSchoolMeals:
                    return GetCheckProcessDataType<CheckEligibilityRequestData_Fsm>(type, data);
                default:
                    throw new NotImplementedException($"Type:-{type} not supported.");
            }
        }

        private static CheckProcessData GetCheckProcessDataType<T>(CheckEligibilityType type, string data) where T : IEligibilityServiceType
        {
            dynamic checkItem = JsonConvert.DeserializeObject(data, typeof(T));
            //CheckEligibilityRequestData_Fsm checkItem = JsonConvert.DeserializeObject<T>(data);
            return new CheckProcessData
            {
                DateOfBirth = checkItem.DateOfBirth,
                LastName = checkItem.LastName.ToUpper(),
                NationalAsylumSeekerServiceNumber = checkItem.NationalAsylumSeekerServiceNumber,
                NationalInsuranceNumber = checkItem.NationalInsuranceNumber,
                Type = type,
            };
        }

        [ExcludeFromCodeCoverage(Justification = "Queue is external dependency.")]
        private void LogQueueCount(QueueClient queue)
        {
            QueueProperties properties = queue.GetProperties();

            // Retrieve the cached approximate message count
            int cachedMessagesCount = properties.ApproximateMessagesCount;
            TrackMetric($"QueueCount:-{_queueClientStandard.Name}", cachedMessagesCount);
        }

        private async Task<CheckEligibilityStatus> HO_Check(CheckProcessData data)
        {
            var checkReults = _db.FreeSchoolMealsHO.Where(x =>
           x.NASS == data.NationalAsylumSeekerServiceNumber
           && x.DateOfBirth ==DateTime.ParseExact(data.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None)).Select(x => x.LastName);
            return CheckSurname(data.LastName, checkReults);
        }

        private async Task<CheckEligibilityStatus> HMRC_Check(CheckProcessData data )
        {
            var checkReults = _db.FreeSchoolMealsHMRC.Where(x =>
            x.FreeSchoolMealsHMRCID == data.NationalInsuranceNumber
            && x.DateOfBirth ==DateTime.ParseExact(data.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None)).Select(x => x.Surname);

            return CheckSurname(data.LastName, checkReults) ;
        }

        private async Task<CheckEligibilityStatus> DWP_Check(CheckProcessData data)
        {
            var checkResult = CheckEligibilityStatus.parentNotFound;
            _logger.LogInformation($"Dwp check use ECS service:- {_dwpService.UseEcsforChecks}");
            if (!_dwpService.UseEcsforChecks)
            {
                checkResult = await DwpCitizenCheck(data, checkResult);
            }
            else
            {
                checkResult = await DwpEcsFsmCheck(data, checkResult);
            }
                
            return checkResult;
        }

        
        private async Task<CheckEligibilityStatus> DwpEcsFsmCheck(CheckProcessData data, CheckEligibilityStatus checkResult)
        {
            //check for benefit
            var result = await _dwpService.EcsFsmCheck(data);
            if (result != null)
            {
                if (result.Status == "1")
                {
                    checkResult = CheckEligibilityStatus.eligible;
                }
                else if (result.Status == "0" && result.ErrorCode == "0" && result.Qualifier.IsNullOrEmpty())
                {
                    checkResult = CheckEligibilityStatus.notEligible;
                }
                else if (result.Status == "0" && result.ErrorCode == "0" && result.Qualifier== "No Trace - Check data")
                {
                    //No Trace - Check data
                    _logger.LogError($"DwpParentNotFound:-{result.Status}, error code:-{result.ErrorCode} qualifier:-{result.Qualifier}. Request:-{JsonConvert.SerializeObject(data)}");
                    checkResult = CheckEligibilityStatus.parentNotFound;
                }
                else
                {
                    _logger.LogError($"DwpError unknown Response status code:-{result.Status}, error code:-{result.ErrorCode} qualifier:-{result.Qualifier}. Request:-{JsonConvert.SerializeObject(data)}");
                    checkResult = CheckEligibilityStatus.DwpError;
                }
            }
            else
            {
                _logger.LogError($"DwpError unknown Response null. Request:-{JsonConvert.SerializeObject(data)}");
                checkResult = CheckEligibilityStatus.DwpError;
            }

            return checkResult;
        }


        private async Task<CheckEligibilityStatus> DwpCitizenCheck(CheckProcessData data, CheckEligibilityStatus checkResult)
        {
            var citizenRequest = new CitizenMatchRequest
            {
                Jsonapi = new CitizenMatchRequest.CitizenMatchRequest_Jsonapi { Version = "2.0" },
                Data = new CitizenMatchRequest.CitizenMatchRequest_Data
                {
                    Type = "Match",
                    Attributes = new CitizenMatchRequest.CitizenMatchRequest_Attributes
                    {
                        LastName = data.LastName,
                        NinoFragment = data.NationalInsuranceNumber,
                        DateOfBirth = data.DateOfBirth
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
                var result = await _dwpService.GetCitizenClaims(guid, DateTime.Now.AddMonths(-3).ToString("yyyy-MMM-dd"), DateTime.Now.ToString("yyyy-MMM-dd"));
                if (result.StatusCode == StatusCodes.Status200OK)
                {
                    checkResult = CheckEligibilityStatus.eligible;
                }
                else if (result.StatusCode == StatusCodes.Status404NotFound)
                {
                    checkResult = CheckEligibilityStatus.notEligible;
                }
                else
                {
                    _logger.LogError($"DwpError unknown Response status code:-{result.StatusCode}. Request:-{JsonConvert.SerializeObject(citizenRequest.Data)}");
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
