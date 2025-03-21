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
using CheckYourEligibility.Domain.Exceptions;

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
        Task<CheckEligibilityResponse> Execute(CheckEligibilityRequest_Fsm model);
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

        public async Task<CheckEligibilityResponse> Execute(CheckEligibilityRequest_Fsm model)
        {
            if (model == null || model.Data == null)
            {
                throw new ValidationException(null, "Invalid Request, data is required.");
            }
            if (model.GetType() != typeof(CheckEligibilityRequest_Fsm))
            {
                throw new ValidationException(null, $"Unknown request type:-{model.GetType()}");
            }

            // Normalize and validate the request
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber?.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator_Fsm();
            var validationResults = validator.Validate(model.Data);

            if (!validationResults.IsValid)
            {
                throw new ValidationException(null, validationResults.ToString());
            }

            // Execute the check
            var response = await _checkService.PostCheck(model.Data);
            if (response != null)
            {
                await _auditService.CreateAuditEntry(AuditType.Check, response.Id);
                _logger.LogInformation($"FSM eligibility check created with ID: {response.Id}");
                return new CheckEligibilityResponse
                {
                    Data = new StatusValue { Status = response.Status.ToString() },
                    Links = new CheckEligibilityResponseLinks
                    {
                        Get_EligibilityCheck = $"{CheckLinks.GetLink}{response.Id}",
                        Put_EligibilityCheckProcess = $"{CheckLinks.ProcessLink}{response.Id}",
                        Get_EligibilityCheckStatus = $"{CheckLinks.GetLink}{response.Id}/status"
                    }
                };
            }
            
            _logger.LogWarning("Response for FSM eligibility check was null.");
            throw new ValidationException(null, "Eligibility check not completed successfully.");
        }
    }
}