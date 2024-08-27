using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.Services
{
    public class HashService : BaseService, IHash
    {

        private readonly ILogger _logger;
        private readonly IEligibilityCheckContext _db;
        private readonly int _hashCheckDays;
        protected readonly IAudit _audit;

        /// <summary>
        /// Manages Check Hashing
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="configuration"></param>
        /// <param name="audit"></param>
        public HashService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IConfiguration configuration, IAudit audit) : base()
        {
            _logger = logger.CreateLogger("HashService");
            _db = Guard.Against.Null(dbContext);
            _hashCheckDays = configuration.GetValue<short>("HashCheckDays");
            _audit = Guard.Against.Null(audit);
        }
              
        /// <summary>
        /// does a hash item exist for a check
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<EligibilityCheckHash?> Exists(EligibilityCheck item)
        {
            var age = DateTime.UtcNow.AddDays(-_hashCheckDays);
            var hash = GetHash(item);
            return await _db.EligibilityCheckHashes.FirstOrDefaultAsync(x => x.Hash == hash && x.TimeStamp >= age);
        }

        /// <summary>
        /// Create the hash item and audit
        /// </summary>
        /// <param name="item"></param>
        /// <param name="outcome"></param>
        /// <param name="source"></param>
        /// <param name="auditDataTemplate"></param>
        /// <returns></returns>
        /// <remarks>NOTE there is no save, Context should be saved in calling service</remarks>
         public async Task<string> Create(EligibilityCheck item, CheckEligibilityStatus outcome, ProcessEligibilityCheckSource source, AuditData auditDataTemplate)
        {
            var hash = GetHash(item);
            var HashItem = new EligibilityCheckHash()
            {
                EligibilityCheckHashID = Guid.NewGuid().ToString(),
                Hash = hash,
                Type = item.Type,
                Outcome = outcome,
                TimeStamp = DateTime.UtcNow,
                Source = source
            };
            item.EligibilityCheckHashID = HashItem.EligibilityCheckHashID;
            await _db.EligibilityCheckHashes.AddAsync(HashItem);

            auditDataTemplate.Type = AuditType.Hash;
            auditDataTemplate.typeId = HashItem.EligibilityCheckHashID;
            await _audit.AuditAdd(auditDataTemplate);

            return item.EligibilityCheckHashID;
        }

        /// <summary>
        /// Get hash identifier for type
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private string GetHash(EligibilityCheck item)
        {
            var key = string.IsNullOrEmpty(item.NINumber) ? item.NASSNumber.ToUpper() : item.NINumber.ToUpper();
            var input = $"{item.LastName.ToUpper()}{key}{item.DateOfBirth.ToString("d")}{item.Type}";
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }

    }
}

