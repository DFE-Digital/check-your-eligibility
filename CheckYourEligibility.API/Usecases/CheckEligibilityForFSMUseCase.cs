using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation.Results;
using System.Threading.Tasks;
using CheckYourEligibility.API.Domain.Exceptions;

namespace CheckYourEligibility.API.UseCases
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
        private readonly ICheckEligibility _checkGateway;
        private readonly IAudit _auditGateway;
        private readonly ILogger<CheckEligibilityForFSMUseCase> _logger;

        public CheckEligibilityForFSMUseCase(
            ICheckEligibility checkGateway,
            IAudit auditGateway,
            ILogger<CheckEligibilityForFSMUseCase> logger)
        {
            _checkGateway = checkGateway;
            _auditGateway = auditGateway;
            _logger = logger;
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
            var response = await _checkGateway.PostCheck(model.Data);
            if (response != null)
            {
                await _auditGateway.CreateAuditEntry(AuditType.Check, response.Id);
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