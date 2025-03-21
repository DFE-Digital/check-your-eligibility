using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CheckYourEligibility.Domain.Exceptions;

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
        Task<CheckEligibilityItemResponse> Execute(string guid);
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
            _checkService = checkService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CheckEligibilityItemResponse> Execute(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ValidationException(null, "Invalid Request, check ID is required.");
            }

            var response = await _checkService.GetItem<CheckEligibilityItem>(guid);
            if (response == null)
            {
                _logger.LogWarning($"Eligibility check with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                throw new NotFoundException(guid);
            }
            await _auditService.CreateAuditEntry(AuditType.Check, guid);
            
            _logger.LogInformation($"Retrieved eligibility check details for ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");

            return new CheckEligibilityItemResponse()
            {
                Data = response,
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{CheckLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{CheckLinks.ProcessLink}{guid}",
                    Get_EligibilityCheckStatus = $"{CheckLinks.GetLink}{guid}/Status"
                }
            };
        }
    }
}