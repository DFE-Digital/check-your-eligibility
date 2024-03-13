// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CheckYourEligibility.Services
{
    public class FsmCheckEligibilityService : IFsmCheckEligibility
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        private readonly QueueClient _queueClient;
        private const int SurnameCheckCharachters = 3;

        public FsmCheckEligibilityService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, QueueServiceClient queueClientService, IConfiguration configuration)
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            var queName = configuration.GetValue<string>("QueueFsmCheckStandard");
            if (queName != "notSet")
            {
                _queueClient = queueClientService.GetQueueClient(queName);
            }
        }

        public async Task<string> PostCheck(CheckEligibilityRequestDataFsm data)
        {
            try
            {
                var item = _mapper.Map<EligibilityCheck>(data);
                item.EligibilityCheckID = Guid.NewGuid().ToString();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;

                item.Status = CheckEligibilityStatus.queuedForProcessing;
                item.Type = CheckEligibilityType.FreeSchoolMeals;

                await _db.FsmCheckEligibilities.AddAsync(item);
                await _db.SaveChangesAsync();
                if (_queueClient != null)
                {
                    await _queueClient.SendMessageAsync(
                        JsonConvert.SerializeObject(new QueueMessageCheck() { Type = item.Type.ToString(), Guid = item.EligibilityCheckID, Url =$"{FSM.ProcessLink}{item.EligibilityCheckID}" }));
                }
                return item.EligibilityCheckID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post");
                throw;
            }
        }

        public async Task<CheckEligibilityStatus?> GetStatus(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x=> x.EligibilityCheckID == guid);
            if (result != null)
                return result.Status;
            return null;
        }

        public async Task<CheckEligibilityStatus?> ProcessCheck(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                
                CheckEligibilityStatus checkResult = CheckEligibilityStatus.parentNotFound;
                if (!result.NINumber.IsNullOrEmpty())
                {
                    checkResult = await HMRC_Check(result);
                }
                else if (!result.NASSNumber.IsNullOrEmpty()) 
                {
                    checkResult = await HO_Check(result);                  
                }
                result.Status = checkResult;
                result.Updated = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return result.Status;
            }
            return null;
        }

        public async Task<CheckEligibilityItemFsm?> GetItem(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                var item = _mapper.Map<CheckEligibilityItemFsm>(result);
                return  item ;
            }
            return null;
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
            var checkReults =  _db.FreeSchoolMealsHMRC.Where(x =>
            x.FreeSchoolMealsHMRCID == data.NINumber 
            && x.DateOfBirth == data.DateOfBirth).Select(x=>x.Surname);

            return CheckSurname(data.LastName, checkReults);
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
    }
}
