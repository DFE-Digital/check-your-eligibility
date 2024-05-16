using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckYourEligibility.Services
{
    public class AuditService : BaseService, IAudit
    {

        private readonly ILogger _logger;
        protected readonly IMapper _mapper;
        private readonly IEligibilityCheckContext _db;
        public AuditService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper) : base()
        {
            _logger = logger.CreateLogger("UsersService");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
        }
      
        public async Task<string> AuditAdd(AuditData data)
        {
            try
            {
                
                var item = _mapper.Map<Audit>(data);
                item.AuditID = Guid.NewGuid().ToString();
                item.TimeStamp = DateTime.UtcNow;

                await _db.Audits.AddAsync(item);
                await _db.SaveChangesAsync();

                return item.AuditID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db Audit");
                throw;
            }

        }
    }
}

