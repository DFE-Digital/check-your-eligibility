// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Data.Models;
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
                        JsonConvert.SerializeObject(new QueueMessageCheck() { Type = item.Type.ToString(), Guid = item.EligibilityCheckID }));
                }
                return item.EligibilityCheckID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post");
                throw;
            }
        }

        public async Task<CheckEligibilityStatusResponse?> GetStatus(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x=> x.EligibilityCheckID == guid);
            if (result != null)
                return new CheckEligibilityStatusResponse { Data = new StatusResponse { Status = result.Status.ToString() } };
            return null;
        }

        public async Task<CheckEligibilityStatusResponse?> ProcessCheck(string guid)
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
                return new CheckEligibilityStatusResponse { Data = new StatusResponse { Status = result.Status.ToString() } };
            }
            return null;
        }

        public async Task<CheckEligibilityItemFsmResponse?> GetItem(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.EligibilityCheckID == guid);
            if (result != null)
            {
                var item = _mapper.Map<CheckEligibilityItemFsm>(result);
                return new CheckEligibilityItemFsmResponse { Data = item } ;
            }
            return null;
        }

        private async Task<CheckEligibilityStatus> HO_Check(EligibilityCheck data)
        {
            var check = await _db.FreeSchoolMealsHO.FirstOrDefaultAsync(x =>
           x.NASS == data.NASSNumber
           && x.LastName == data.LastName
           && x.DateOfBirth == data.DateOfBirth);
            if (check != null)
            {
                return CheckEligibilityStatus.eligible;
            };

            return CheckEligibilityStatus.parentNotFound;
        }

        private async Task<CheckEligibilityStatus> HMRC_Check(EligibilityCheck data)
        {
            var check = await _db.FreeSchoolMealsHMRC.FirstOrDefaultAsync(x =>
            x.FreeSchoolMealsHMRCID == data.NINumber 
            && x.Surname == data.LastName 
            && x.DateOfBirth == data.DateOfBirth);
            if (check != null) 
            { 
                return CheckEligibilityStatus.eligible; 
            };

            return CheckEligibilityStatus.parentNotFound;
        }

        
    }
}
