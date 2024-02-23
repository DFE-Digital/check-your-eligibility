// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;

namespace CheckYourEligibility.Services
{
    public class FsmCheckEligibilityService : IFsmCheckEligibility
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;


        public FsmCheckEligibilityService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper)
        {
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            
        }

        public async Task<string> PostCheck(CheckEligibilityRequestDataFsm data)
        {
            try
            {
                var item = _mapper.Map<FsmCheckEligibility>(data);
                item.FsmCheckEligibilityID = Guid.NewGuid().ToString();
                item.Status = FsmCheckEligibilityStatus.queuedForProcessing;

                await _db.FsmCheckEligibilities.AddAsync(item);
                _db.SaveChangesAsync();
                _logger.LogInformation($"Posted {item.FsmCheckEligibilityID}.");
                return item.FsmCheckEligibilityID; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post");
                throw;
            }
        }

        public async Task<CheckEligibilityStatusResponse?> GetStatus(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x=> x.FsmCheckEligibilityID == guid);
            if (result != null)
                return new CheckEligibilityStatusResponse { Data = new Domain.Requests.Data { Status = result.Status.ToString() } };
            return null;
        }

        public async Task<CheckEligibilityStatusResponse?> Process(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.FsmCheckEligibilityID == guid);
            if (result != null)
            {
                result.Status = FsmCheckEligibilityStatus.parentNotFound;
                if (!result.NINumber.IsNullOrEmpty())
                {
                    var hmrcCheck = await HMRC_Check(result);
                    result.Status = hmrcCheck;
                }
                
                _db.SaveChangesAsync();
                return new CheckEligibilityStatusResponse { Data = new Domain.Requests.Data { Status = result.Status.ToString() } };
            }
            return null;
        }

        private async Task<FsmCheckEligibilityStatus> HMRC_Check(FsmCheckEligibility data)
        {
            var check = await _db.FreeSchoolMealsHMRC.FirstOrDefaultAsync(x =>
            x.FreeSchoolMealsHMRCID == data.NINumber 
            && x.Surname == data.LastName 
            && x.DateOfBirth == data.DateOfBirth);
            if (check != null) 
            { 
                return FsmCheckEligibilityStatus.eligible; 
            };

            return FsmCheckEligibilityStatus.parentNotFound;
        }
    }
}
