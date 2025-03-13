using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CheckYourEligibility.Services
{
    public class AuditService : BaseService, IAudit
    {

        private readonly ILogger _logger;
        protected readonly IMapper _mapper;
        private readonly IEligibilityCheckContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuditService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base()
        {
            _logger = logger.CreateLogger("UsersService");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            _httpContextAccessor = Guard.Against.Null(httpContextAccessor);
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

        public AuditData? AuditDataGet(AuditType type, string id)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    var host = httpContext.Request.Host;
                    var path = httpContext.Request.Path;
                    var method = httpContext.Request.Method;
                    var auth = httpContext.User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? "";
                    var scope = httpContext.User.Claims.FirstOrDefault(x => x.Type == "scope")?.Value ?? "";
                    return new AuditData
                    {
                        Type = type,
                        typeId = id,
                        url = $"{host}{path}",
                        method = method,
                        source = remoteIpAddress.ToString(),
                        authentication = auth,
                        scope = scope
                    };
                }
                return new AuditData
                {
                    Type = type,
                    typeId = id,
                    url = "Unknown",
                    method = "Unknown",
                    source = "Unknown",
                    authentication = "Unknown",
                    scope = "Unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit data for type {Type} and ID {Id}", type, id);
                return null;
            }
        }

        public async Task<string> CreateAuditEntry(AuditType type, string id)
        {
            try
            {
                // Get the audit data
                var auditData = AuditDataGet(type, id);
                if (auditData == null)
                {
                    _logger.LogWarning("Failed to create audit data for type {Type} and ID {Id}", type, id);
                    return string.Empty;
                }

                // Add it to the database
                return await AuditAdd(auditData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit entry for type {Type} and ID {Id}", type, id);
                return string.Empty;
            }
        }
    }
}

