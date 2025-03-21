using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CheckYourEligibility.Domain.Exceptions;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for updating eligibility check status
    /// </summary>
    public interface IUpdateEligibilityCheckStatusUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The ID of the eligibility check</param>
        /// <param name="model">The status update request</param>
        /// <returns>Updated eligibility check status</returns>
        Task<CheckEligibilityStatusResponse> Execute(string guid, EligibilityStatusUpdateRequest model);
    }

    public class UpdateEligibilityCheckStatusUseCase : IUpdateEligibilityCheckStatusUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<UpdateEligibilityCheckStatusUseCase> _logger;

        public UpdateEligibilityCheckStatusUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<UpdateEligibilityCheckStatusUseCase> logger)
        {
            _checkService = checkService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CheckEligibilityStatusResponse> Execute(string guid, EligibilityStatusUpdateRequest model)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ValidationException(null, "Invalid Request, check ID is required.");
            }

            if (model == null || model.Data == null)
            {
                throw new ValidationException(null, "Invalid Request, update data is required.");
            }

            var response = await _checkService.UpdateEligibilityCheckStatus(guid, model.Data);
            if (response == null)
            {
                _logger.LogWarning($"Failed to update eligibility check status for ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
                throw new NotFoundException(guid);
            }

            await _auditService.CreateAuditEntry(AuditType.Check, guid);

            _logger.LogInformation($"Updated eligibility check status for ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            return new CheckEligibilityStatusResponse
            {
                Data = response.Data
            };
        }
    }
}