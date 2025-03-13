using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for retrieving eligibility check item details
    /// </summary>
    public interface IGetEligibilityCheckItemUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The ID of the eligibility check</param>
        /// <returns>Eligibility check item details</returns>
        Task<UseExecutionResult<CheckEligibilityItemResponse>> Execute(string guid);
    }

    public class GetEligibilityCheckItemUseCase : IGetEligibilityCheckItemUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<GetEligibilityCheckItemUseCase> _logger;

        public GetEligibilityCheckItemUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<GetEligibilityCheckItemUseCase> logger)
        {
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityItemResponse>> Execute(string guid)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityItemResponse>();

            if (string.IsNullOrEmpty(guid))
            {
                useCaseExecutionResult.SetFailure("Invalid Request, check ID is required.");
                return useCaseExecutionResult;
            }

            var response = await _checkService.GetItem<CheckEligibilityItem>(guid);
            if (response == null)
            {
                _logger.LogWarning($"Eligibility check with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                useCaseExecutionResult.SetNotFound(guid);
                return useCaseExecutionResult;
            }
            await _auditService.CreateAuditEntry(AuditType.Check, guid);
            
            _logger.LogInformation($"Retrieved eligibility check details for ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            useCaseExecutionResult.SetSuccess(new CheckEligibilityItemResponse()
            {
                Data = response,
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{CheckLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{CheckLinks.ProcessLink}{guid}",
                    Get_EligibilityCheckStatus = $"{CheckLinks.GetLink}{guid}/Status"
                }
            });
            
            return useCaseExecutionResult;
        }
    }
}