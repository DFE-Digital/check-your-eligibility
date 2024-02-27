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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                item.Created = DateTime.UtcNow;
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

        public async Task<CheckEligibilityStatusResponse?> ProcessCheck(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.FsmCheckEligibilityID == guid);
            if (result != null)
            {
                
                FsmCheckEligibilityStatus checkResult = FsmCheckEligibilityStatus.parentNotFound;
                if (!result.NINumber.IsNullOrEmpty())
                {
                    checkResult = await HMRC_Check(result);
                }
                else if (!result.NASSNumber.IsNullOrEmpty()) 
                {
                    checkResult = await HO_Check(result);                  
                }
                result.Status = checkResult;
                _db.SaveChangesAsync();
                return new CheckEligibilityStatusResponse { Data = new Domain.Requests.Data { Status = result.Status.ToString() } };
            }
            return null;
        }

        public async Task<CheckEligibilityItemFsmResponse?> GetItem(string guid)
        {
            var result = await _db.FsmCheckEligibilities.FirstOrDefaultAsync(x => x.FsmCheckEligibilityID == guid);
            if (result != null)
            {
                var item = _mapper.Map<CheckEligibilityItemFsm>(result);
                return new CheckEligibilityItemFsmResponse { Data = item } ;
            }
            return null;
        }

        private async Task<FsmCheckEligibilityStatus> HO_Check(FsmCheckEligibility data)
        {
            var check = await _db.FreeSchoolMealsHO.FirstOrDefaultAsync(x =>
           x.NASS == data.NASSNumber
           && x.LastName == data.LastName
           && x.DateOfBirth == data.DateOfBirth);
            if (check != null)
            {
                return FsmCheckEligibilityStatus.eligible;
            };

            return FsmCheckEligibilityStatus.parentNotFound;
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
