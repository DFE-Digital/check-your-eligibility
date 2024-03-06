// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using AutoMapper;
using Azure.Storage.Queues;
using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.Services
{
    public class AdministrationService : IAdministration
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        private readonly IConfiguration _configuration;
       
        public AdministrationService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IConfiguration configuration)
        {
            _logger = logger.CreateLogger("ServiceAdministration");
            _db = Guard.Against.Null(dbContext);
            _configuration = Guard.Against.Null(configuration);
        }

        public async Task CleanUpEligibilityChecks()
        {
            var checkDate = DateTime.UtcNow.AddDays(-_configuration.GetValue<int>($"DataCleanseDaysSoftCheck_Status_{CheckEligibilityStatus.eligible}"));
            var items = _db.FsmCheckEligibilities.Where(x => x.Created <= checkDate);
            _db.FsmCheckEligibilities.RemoveRange(items);
            await _db.SaveChangesAsync();

            checkDate = DateTime.UtcNow.AddDays(-_configuration.GetValue<int>($"DataCleanseDaysSoftCheck_Status_{CheckEligibilityStatus.parentNotFound}"));
            items = _db.FsmCheckEligibilities.Where(x => x.Created <= checkDate);
            _db.FsmCheckEligibilities.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
        
    }
}
