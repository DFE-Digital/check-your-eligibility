using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for retrieving bulk upload results
    /// </summary>
    public interface IGetBulkUploadResultsUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The group ID of the bulk upload</param>
        /// <returns>Bulk upload results</returns>
        Task<CheckEligibilityBulkResponse> Execute(string guid);
    }

    public class GetBulkUploadResultsUseCase : IGetBulkUploadResultsUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly IAudit _auditService;
        private readonly ILogger<GetBulkUploadResultsUseCase> _logger;

        public GetBulkUploadResultsUseCase(
            ICheckEligibility checkService,
            IAudit auditService,
            ILogger<GetBulkUploadResultsUseCase> logger)
        {
            _checkService = checkService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<CheckEligibilityBulkResponse> Execute(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ValidationException(null, "Invalid Request, group ID is required.");
            }

            var response = await _checkService.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid);
            if (response == null)
            {
                _logger.LogWarning($"Bulk upload results with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                throw new NotFoundException(guid);
            }
            
            await _auditService.CreateAuditEntry(AuditType.CheckBulkResults, guid);
            
            _logger.LogInformation($"Retrieved bulk upload results for group ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            return new CheckEligibilityBulkResponse()
            {
                Data = response as List<CheckEligibilityItem>
            };
        }
    }
}