using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
        Task<UseExecutionResult<CheckEligibilityStatusResponse>> Execute(string guid, EligibilityStatusUpdateRequest model);
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
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityStatusResponse>> Execute(string guid, EligibilityStatusUpdateRequest model)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();

            if (string.IsNullOrEmpty(guid))
            {
                useCaseExecutionResult.SetFailure("Invalid Request, check ID is required.");
                return useCaseExecutionResult;
            }

            if (model == null || model.Data == null)
            {
                useCaseExecutionResult.SetFailure("Invalid Request, update data is required.");
                return useCaseExecutionResult;
            }

            var response = await _checkService.UpdateEligibilityCheckStatus(guid, model.Data);
            if (response == null)
            {
                _logger.LogWarning($"Failed to update eligibility check status for ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
                useCaseExecutionResult.SetNotFound(guid);
                return useCaseExecutionResult;
            }

            var auditData = _auditService.AuditDataGet(AuditType.Check, guid);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }

            _logger.LogInformation($"Updated eligibility check status for ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            useCaseExecutionResult.SetSuccess(new CheckEligibilityStatusResponse
            {
                Data = response.Data
            });
            
            return useCaseExecutionResult;
        }
    }
}