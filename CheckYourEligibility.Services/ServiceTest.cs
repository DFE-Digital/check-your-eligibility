using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CheckYourEligibility.Services
{
    public class ServiceTest : IServiceTest
    {
        private  readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;

        //public ServiceTest()
        //{
        //}

        public ServiceTest(ILoggerFactory logger, IEligibilityCheckContext dbContext)
        {
            logger = Guard.Against.Null(logger);
            _db = Guard.Against.Null(dbContext);

            _logger = logger.CreateLogger("ServiceTest");
        }

        public async Task<List<Student>> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("GET Pages.ContactModel called.");
               //var x = _db.Students.FirstOrDefault();
                var result =  await _db.Students.ToListAsync();
                return  result;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

    }
}
