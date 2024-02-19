// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CheckYourEligibility.Services
{
    public class FsmCheckEligibility : IFsmCheckEligibility
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        protected readonly IMapper _mapper;


        public FsmCheckEligibility(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper)
        {
            Guard.Against.Null(logger);
            _logger = logger.CreateLogger("ServiceFsmCheckEligibility");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            
        }

        public async Task<string> PostCheck(CheckEligibilityRequestData data)
        {
            try
            {
                var item = _mapper.Map<Data.Models.FsmCheckEligibility>(data);
                item.FsmCheckEligibilityID = Guid.NewGuid().ToString();
                item.Status = FsmCheckEligibilityStatus.queuedForProcessing;

                await _db.FsmCheckEligibilities.AddAsync(item);
                _db.SaveChanges();
                _logger.LogInformation($"Posted {item.FsmCheckEligibilityID}.");
                return item.FsmCheckEligibilityID; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post");
                throw;
            }
        }
    }
}
