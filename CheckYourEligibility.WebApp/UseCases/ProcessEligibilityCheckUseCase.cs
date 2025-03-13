using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for processing eligibility checks
    /// </summary>
    public interface IProcessEligibilityCheckUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The ID of the eligibility check</param>
        /// <returns>Processed eligibility check status</returns>
        Task<UseExecutionResult<CheckEligibilityStatusResponse>> Execute(string guid);
    }

    public class ProcessEligibilityCheckUseCase : IProcessEligibilityCheckUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<ProcessEligibilityCheckUseCase> _logger;

        public ProcessEligibilityCheckUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<ProcessEligibilityCheckUseCase> logger)
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

            try
            {
                var auditItemTemplate = _auditService.AuditDataGet(AuditType.Check, string.Empty);
                var response = await _checkService.ProcessCheck(guid, auditItemTemplate);
                
                if (response == null)
                {
                    _logger.LogWarning($"Eligibility check with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                    useCaseExecutionResult.SetNotFound(guid);
                    return useCaseExecutionResult;
                }

                await _auditService.CreateAuditEntry(AuditType.Check, guid);
                
                _logger.LogInformation($"Processed eligibility check with ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}, status: {response.Value}");
                
                var resultResponse = new CheckEligibilityStatusResponse() 
                { 
                    Data = new StatusValue() 
                    { 
                        Status = response.Value.ToString() 
                    } 
                };
                
                if (response.Value == CheckEligibilityStatus.queuedForProcessing)
                {
                    useCaseExecutionResult.SetServiceUnavailable();
                }
                else
                {
                    useCaseExecutionResult.SetSuccess(resultResponse);
                }
            }
            catch (ProcessCheckException ex)
            {
                _logger.LogError(ex, $"Error processing eligibility check with ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
                useCaseExecutionResult.SetFailure("Failed to process eligibility check.");
            }
            
            return useCaseExecutionResult;
        }
    }
}