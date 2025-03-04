using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
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
        Task<UseExecutionResult<CheckEligibilityStatusResponse>> Execute(string guid);
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
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityStatusResponse>> Execute(string guid)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityStatusResponse>();

            if (string.IsNullOrEmpty(guid))
            {
                useCaseExecutionResult.SetFailure("Invalid Request, check ID is required.");
                return useCaseExecutionResult;
            }

            var response = await _checkService.GetStatus(guid);
            if (response == null)
            {
                _logger.LogWarning($"Eligibility check with ID {guid} not found");
                useCaseExecutionResult.SetNotFound(guid);
                return useCaseExecutionResult;
            }

            var auditData = _auditService.AuditDataGet(AuditType.Check, guid);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }

            _logger.LogInformation($"Retrieved eligibility check status for ID: {guid}");
            
            useCaseExecutionResult.SetSuccess(new CheckEligibilityStatusResponse() 
            { 
                Data = new StatusValue() 
                { 
                    Status = response.Value.ToString() 
                } 
            });
            
            return useCaseExecutionResult;
        }
    }
}