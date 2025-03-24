﻿// Ignore Spelling: Fsm
using AutoMapper;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Domain.Exceptions;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Requests.DWP;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.API.Gateways
{
    public partial class CheckEligibilityGateway : BaseGateway, ICheckEligibility
    {
        private QueueClient _queueClientStandard;
        private QueueClient _queueClientBulk;
        private const int SurnameCheckCharachters = 3;
        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        private readonly IConfiguration _configuration;
        protected readonly IMapper _mapper;
        protected readonly IAudit _audit;

        private readonly IDwpGateway _dwpGateway;
        private readonly IHash _hashGateway;
        private string _groupId;

        public CheckEligibilityGateway(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, QueueServiceClient queueClientGateway,
            IConfiguration configuration, IDwpGateway dwpGateway, IAudit audit, IHash hashGateway) : base()
        {
            _logger = logger.CreateLogger("ServiceCheckEligibility");
            _db = dbContext;
            _mapper = mapper;
            _dwpGateway = dwpGateway;
            _audit = audit;
            _hashGateway = hashGateway;
            _configuration = configuration;

            setQueueStandard(_configuration.GetValue<string>("QueueFsmCheckStandard"), queueClientGateway);
            setQueueBulk(_configuration.GetValue<string>("QueueFsmCheckBulk"), queueClientGateway);
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

                var checkHashResult = await _hashGateway.Exists(JsonConvert.DeserializeObject<CheckProcessData>(item.CheckData));
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
                    var queue = await SendMessage(item);
                }

                return new PostCheckResult { Id = item.EligibilityCheckID, Status = item.Status };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post");
                throw;
            }
        }

        public async Task<CheckEligibilityStatus?> GetStatus(string guid)
        {
            var result = await _db.CheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
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
                var checkData = GetCheckProcessData(result.Type, result.CheckData);
                if (result.Status != CheckEligibilityStatus.queuedForProcessing)
                {
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
            IList<CheckEligibilityItem> items = new List<CheckEligibilityItem>();
            var resultList = _db.CheckEligibilities
                .Where(x => x.Group == guid)
                .OrderBy(x => x.Sequence);
            if (resultList != null && resultList.Any())
            {
                var type = typeof(T);
                if (type == typeof(IList<CheckEligibilityItem>))
                {
                    foreach(var result in resultList)
                    {
                        CheckProcessData data = GetCheckProcessData(result.Type, result.CheckData);
                        items.Add(new CheckEligibilityItem()
                        {
                            Status = result.Status.ToString(),
                            Created = result.Created,
                            NationalInsuranceNumber = data.NationalInsuranceNumber,
                            LastName = data.LastName,
                            DateOfBirth = data.DateOfBirth,
                            NationalAsylumSeekerServiceNumber = data.NationalAsylumSeekerServiceNumber
                        });
                    }

                    return (T)items;
                }
                else
                {
                    throw new Exception($"unable to cast to type {type}");
                }
            }
            return default;
        }

        public static string GetHash(CheckProcessData item)
        {
            var key = string.IsNullOrEmpty(item.NationalInsuranceNumber) ? item.NationalAsylumSeekerServiceNumber.ToUpper() : item.NationalInsuranceNumber.ToUpper();
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
                .GroupBy(n => n.Status)
                .Select(n => new { Status = n.Key, ct = n.Count() });
            if (results.Any())
            {
                return new BulkStatus { Total = results.Sum(s => s.ct), Complete = results.Where(a => a.Status != CheckEligibilityStatus.queuedForProcessing).Sum(s => s.ct) };
            }
            return null;
        }

        #region Private
        [ExcludeFromCodeCoverage]
        private void setQueueStandard(string queName, QueueServiceClient queueClientGateway)
        {
            if (queName != "notSet")
            {
                _queueClientStandard = queueClientGateway.GetQueueClient(queName);
            }
        }

        [ExcludeFromCodeCoverage]
        private void setQueueBulk(string queName, QueueServiceClient queueClientGateway)
        {
            if (queName != "notSet")
            {
                _queueClientBulk = queueClientGateway.GetQueueClient(queName);
            }
        }

        [ExcludeFromCodeCoverage(Justification = "Queue is external dependency.")]
        private async Task<string> SendMessage(EligibilityCheck item)
        {
            var queueName = string.Empty;
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
                    queueName = _queueClientStandard.Name;

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
                    queueName = _queueClientBulk.Name;
                }
            }
            return queueName;
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

            if (checkResult == CheckEligibilityStatus.Error)
            {
                // Revert status back and do not save changes
                result.Status = CheckEligibilityStatus.queuedForProcessing;
                TrackMetric($"Dwp Error", 1);
            }
            else
            {
                result.EligibilityCheckHashID = await _hashGateway.Create(checkData, checkResult, source, auditDataTemplate);
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
           && x.DateOfBirth == DateTime.ParseExact(data.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None)).Select(x => x.LastName);
            return CheckSurname(data.LastName, checkReults);
        }

        private async Task<CheckEligibilityStatus> HMRC_Check(CheckProcessData data)
        {
            var checkReults = _db.FreeSchoolMealsHMRC.Where(x =>
            x.FreeSchoolMealsHMRCID == data.NationalInsuranceNumber
            && x.DateOfBirth == DateTime.ParseExact(data.DateOfBirth, "yyyy-MM-dd", null, DateTimeStyles.None)).Select(x => x.Surname);

            return CheckSurname(data.LastName, checkReults);
        }

        private async Task<CheckEligibilityStatus> DWP_Check(CheckProcessData data)
        {
            var checkResult = CheckEligibilityStatus.parentNotFound;
            _logger.LogInformation($"Dwp check use ECS service:- {_dwpGateway.UseEcsforChecks}");
            if (!_dwpGateway.UseEcsforChecks)
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
            var result = await _dwpGateway.EcsFsmCheck(data);
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
                else if (result.Status == "0" && result.ErrorCode == "0" && result.Qualifier == "No Trace - Check data")
                {
                    checkResult = CheckEligibilityStatus.parentNotFound;
                }
                else
                {
                    _logger.LogError($"Error unknown Response status code:-{result.Status}, error code:-{result.ErrorCode} qualifier:-{result.Qualifier}");
                    checkResult = CheckEligibilityStatus.Error;
                }
            }
            else
            {
                _logger.LogError($"Error ECS unknown Response of null");
                checkResult = CheckEligibilityStatus.Error;
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
            var guid = await _dwpGateway.GetCitizen(citizenRequest);
            if (!Guid.TryParse(guid, out _))
            {
                return (CheckEligibilityStatus)Enum.Parse(typeof(CheckEligibilityStatus), guid);
            }

            if (!string.IsNullOrEmpty(guid))
            {
                //check for benefit
                var result = await _dwpGateway.GetCitizenClaims(guid, DateTime.Now.AddMonths(-3).ToString("yyyy-MMM-dd"), DateTime.Now.ToString("yyyy-MMM-dd"));
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
                    _logger.LogError($"Error unknown Response status code:-{result.StatusCode}.");
                    checkResult = CheckEligibilityStatus.Error;
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

        public async Task ProcessQueue(string queName)
        {

            QueueClient queue;
            if (queName == _configuration.GetValue<string>("QueueFsmCheckStandard"))
            {
                queue = _queueClientStandard;
            }
            else if (queName == _configuration.GetValue<string>("QueueFsmCheckBulk"))
            {
                queue = _queueClientBulk;
            }
            else
            {
                throw new Exception($"invalid queue {queName}.");
            }
            if (await queue.ExistsAsync())
            {
                QueueProperties properties = await queue.GetPropertiesAsync();
                
                while (properties.ApproximateMessagesCount > 0)
                {
                    QueueMessage[] retrievedMessage = await queue.ReceiveMessagesAsync(32);
                    foreach (var item in retrievedMessage)
                    {
                        var checkData = JsonConvert.DeserializeObject<QueueMessageCheck>(Encoding.UTF8.GetString(item.Body));
                        try
                        {
                            var result = await ProcessCheck(checkData.Guid, new AuditData
                            {
                                Type = AuditType.Check,
                                typeId = checkData.Guid,
                                authentication = queName,
                                method = "processQue",
                                source = "queueProcess",
                                url = "."
                            });
                            if (result == null || result != CheckEligibilityStatus.queuedForProcessing || item.DequeueCount > 1) {
                                if (result == null || item.DequeueCount > 1)
                                {
                                    await UpdateEligibilityCheckStatus(checkData.Guid, new EligibilityCheckStatusData { Status = CheckEligibilityStatus.Error });
                                }
                                await queue.DeleteMessageAsync(item.MessageId, item.PopReceipt);
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Queue processing");
                            await queue.DeleteMessageAsync(item.MessageId, item.PopReceipt);
                        }
                        
                    }
                    properties = await queue.GetPropertiesAsync();
                }

            }
        }


        #endregion
    }
}
