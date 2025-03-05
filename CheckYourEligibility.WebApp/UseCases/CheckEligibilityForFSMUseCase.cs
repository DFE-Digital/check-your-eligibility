using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation.Results;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for processing a single FSM eligibility check
    /// </summary>
    public interface ICheckEligibilityForFSMUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="model">FSM eligibility check request</param>
        /// <returns>Check eligibility response or validation errors</returns>
        Task<UseExecutionResult<CheckEligibilityResponse>> Execute(CheckEligibilityRequest_Fsm model);
    }

    public class CheckEligibilityForFSMUseCase : ICheckEligibilityForFSMUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<CheckEligibilityForFSMUseCase> _logger;

        public CheckEligibilityForFSMUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<CheckEligibilityForFSMUseCase> logger)
        {
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityResponse>> Execute(CheckEligibilityRequest_Fsm model)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityResponse>();

            if (model == null || model.Data == null)
            {
                useCaseExecutionResult.SetFailure("Invalid Request, data is required.");
                return useCaseExecutionResult;
            }
            if (model.GetType() != typeof(CheckEligibilityRequest_Fsm))
            {
                useCaseExecutionResult.SetFailure($"Unknown request type:-{model.GetType()}");
                return useCaseExecutionResult;
            }

            // Normalize and validate the request
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber?.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator_Fsm();
            var validationResults = validator.Validate(model.Data);

            if (!validationResults.IsValid)
            {
                _logger.LogWarning("Validation failed for FSM eligibility check");
                useCaseExecutionResult.SetFailure(validationResults.ToString());
                return useCaseExecutionResult;
            }

            // Execute the check
            var response = await _checkService.PostCheck(model.Data);
            var auditData = _auditService.AuditDataGet(AuditType.Check, response.Id);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }

            _logger.LogInformation($"FSM eligibility check created with ID: {response.Id}");
            useCaseExecutionResult.SetSuccess(new CheckEligibilityResponse
            {
                Data = new StatusValue { Status = response.Status.ToString() },
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{CheckLinks.GetLink}{response.Id}",
                    Put_EligibilityCheckProcess = $"{CheckLinks.ProcessLink}{response.Id}",
                    Get_EligibilityCheckStatus = $"{CheckLinks.GetLink}{response.Id}/Status"
                }
            });

            return useCaseExecutionResult;
        }
    }
}