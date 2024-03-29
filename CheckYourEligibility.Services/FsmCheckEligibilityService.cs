﻿// Ignore Spelling: Fsm

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
        const int referenceMaxValue = 99999999;
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;
        private readonly QueueClient _queueClient;
        private const int SurnameCheckCharachters = 3;
        private static Random randomNumber;

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
            if (randomNumber == null)
            {
               randomNumber = new Random(referenceMaxValue);
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

        public async Task<ApplicationSaveFsm> PostApplication(ApplicationRequestDataFsm data)
        {
            try
            {
                var item = _mapper.Map<Application>(data);
                item.ApplicationID = Guid.NewGuid().ToString();
                item.Reference = GetReference();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;
                item.Type = CheckEligibilityType.ApplcicationFsm;

                var school = _db.Schools.Include(x => x.LocalAuthority).First(x=>x.SchoolId == data.School);
                item.LocalAuthorityId = school.LocalAuthorityId;

                await _db.Applications.AddAsync(item);

                var status = new Data.Models.ApplicationStatus() {
                    ApplicationStatusID = Guid.NewGuid().ToString(),
                    ApplicationID = item.ApplicationID,
                    Type = Domain.Enums.ApplicationStatus.Open,
                    TimeStamp = DateTime.UtcNow };
                await _db.ApplicationStatuses.AddAsync(status);

                await _db.SaveChangesAsync();

                var saved = _db.Applications
                    .First(x=>x.ApplicationID == item.ApplicationID);

                var returnItem = _mapper.Map<ApplicationSaveFsm>(item);

                return returnItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post application");
                throw;
            }
        }

        private string GetReference()
        {
            var unique = false;
            string nextReference = string.Empty;
             while (!unique)
            {
                nextReference = randomNumber.Next(1, referenceMaxValue).ToString();
                unique =   _db.Applications.FirstOrDefault(x => x.Reference == nextReference)== null;
            }

            return nextReference;
        }

        public async Task<ApplicationFsm?> GetApplication(string guid)
        {
            var result = await _db.Applications
                .Include(x => x.Statuses)
                .Include(x => x.School)
                .ThenInclude(x => x.LocalAuthority)
                .FirstOrDefaultAsync(x => x.ApplicationID == guid);
            if (result != null)
            {
                var item = _mapper.Map<ApplicationFsm>(result);
                item.School = new ApplicationFsm.ApplicationSchool()
                {
                    Id = result.SchoolId,
                    Name = result.School.EstablishmentName,
                    LocalAuthority = new ApplicationFsm.ApplicationSchool.SchoolLocalAuthority {
                        Id = result.School.LocalAuthority.LocalAuthorityId,
                        Name = result.School.LocalAuthority.LaName }
                };
                return item;
            }

            return null;
        }
    }
}
