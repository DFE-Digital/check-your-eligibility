using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for retrieving eligibility check status
    /// </summary>
    public interface IGetEligibilityCheckStatusUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The ID of the eligibility check</param>
        /// <returns>Eligibility check status</returns>
        Task<CheckEligibilityStatusResponse> Execute(string guid);
    }

    public class GetEligibilityCheckStatusUseCase : IGetEligibilityCheckStatusUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<GetEligibilityCheckStatusUseCase> _logger;

        public GetEligibilityCheckStatusUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<GetEligibilityCheckStatusUseCase> logger)
        {
            _checkService = checkService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CheckEligibilityStatusResponse> Execute(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ValidationException(null, "Invalid Request, check ID is required.");
            }

            var response = await _checkService.GetStatus(guid);
            if (response == null)
            {
                _logger.LogWarning($"Eligibility check with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                throw new NotFoundException(guid.ToString());
            }

            await _auditService.CreateAuditEntry(AuditType.Check, guid);
            
            _logger.LogInformation($"Retrieved eligibility check status for ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            return new CheckEligibilityStatusResponse() 
            { 
                Data = new StatusValue() 
                { 
                    Status = response.Value.ToString() 
                } 
            };
        }
    }
}