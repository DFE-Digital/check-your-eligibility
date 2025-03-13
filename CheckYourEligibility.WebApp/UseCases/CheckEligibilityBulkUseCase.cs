using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for processing bulk FSM eligibility checks
    /// </summary>
    public interface ICheckEligibilityBulkUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="model">Bulk FSM eligibility check request</param>
        /// <param name="recordCountLimit">Maximum allowed records in a bulk upload</param>
        /// <returns>Check eligibility bulk response or validation errors</returns>
        Task<UseExecutionResult<CheckEligibilityResponseBulk>> Execute(
            CheckEligibilityRequestBulk_Fsm model,
            int recordCountLimit);
    }

    public class CheckEligibilityBulkUseCase : ICheckEligibilityBulkUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<CheckEligibilityBulkUseCase> _logger;

        public CheckEligibilityBulkUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<CheckEligibilityBulkUseCase> logger)
        {
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityResponseBulk>> Execute(
            CheckEligibilityRequestBulk_Fsm model,
            int recordCountLimit)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityResponseBulk>();

            if (model == null || model.Data == null)
            {
                useCaseExecutionResult.SetFailure("Invalid Request, data is required.");
                return useCaseExecutionResult;
            }

            if (model.Data.Count() > recordCountLimit)
            {
                var errorMessage = $"Invalid Request, data limit of {recordCountLimit} exceeded, {model.Data.Count()} records.";
                _logger.LogWarning(errorMessage);
                useCaseExecutionResult.SetFailure(errorMessage);
                return useCaseExecutionResult;
            }

            if (model.GetType() != typeof(CheckEligibilityRequestBulk_Fsm))
            {
                useCaseExecutionResult.SetFailure($"Unknown request type:-{model.GetType()}");
                return useCaseExecutionResult;
            }

            var validationErrors = ValidateBulkItems(model);
            if (validationErrors.Length > 0)
            {
                _logger.LogWarning("Validation errors in bulk eligibility check");
                useCaseExecutionResult.SetFailure(validationErrors.ToString());
                return useCaseExecutionResult;
            }

            var groupId = Guid.NewGuid().ToString();
            await _checkService.PostCheck(model.Data, groupId);
            
            await _auditService.CreateAuditEntry(AuditType.BulkCheck, groupId);
            

            _logger.LogInformation($"Bulk eligibility check created with group ID: {groupId}");

            useCaseExecutionResult.SetSuccess(new CheckEligibilityResponseBulk
            {
                Data = new StatusValue { Status = $"{Messages.Processing}" },
                Links = new CheckEligibilityResponseBulkLinks
                {
                    Get_Progress_Check = $"{CheckLinks.BulkCheckLink}{groupId}{CheckLinks.BulkCheckProgress}",
                    Get_BulkCheck_Results = $"{CheckLinks.BulkCheckLink}{groupId}{CheckLinks.BulkCheckResults}"
                }
            });

            return useCaseExecutionResult;
        }

        private static StringBuilder ValidateBulkItems(CheckEligibilityRequestBulk_Fsm model)
        {
            var validationResultsItems = new StringBuilder();
            var validator = new CheckEligibilityRequestDataValidator_Fsm();
            var sequence = 1;

            foreach (var item in model.Data)
            {
                item.NationalInsuranceNumber = item.NationalInsuranceNumber?.ToUpper();
                item.NationalAsylumSeekerServiceNumber = item.NationalAsylumSeekerServiceNumber?.ToUpper();
                item.Sequence = sequence;

                var validationResults = validator.Validate(item);
                if (!validationResults.IsValid)
                {
                    validationResultsItems.AppendLine($"Item:-{sequence}, {validationResults.ToString()}");
                }

                sequence++;
            }

            return validationResultsItems;
        }
    }
}