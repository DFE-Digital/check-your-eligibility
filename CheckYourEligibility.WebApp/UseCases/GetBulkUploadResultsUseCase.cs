using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
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
        Task<UseExecutionResult<CheckEligibilityBulkResponse>> Execute(string guid);
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
            _checkService = Guard.Against.Null(checkService);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<UseExecutionResult<CheckEligibilityBulkResponse>> Execute(string guid)
        {
            var useCaseExecutionResult = new UseExecutionResult<CheckEligibilityBulkResponse>();

            if (string.IsNullOrEmpty(guid))
            {
                useCaseExecutionResult.SetFailure("Invalid Request, group ID is required.");
                return useCaseExecutionResult;
            }

            var response = await _checkService.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid);
            if (response == null)
            {
                _logger.LogWarning($"Bulk upload results with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                useCaseExecutionResult.SetNotFound(guid);
                return useCaseExecutionResult;
            }
            
            await _auditService.CreateAuditEntry(AuditType.CheckBulkResults, guid);
            
            _logger.LogInformation($"Retrieved bulk upload results for group ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            useCaseExecutionResult.SetSuccess(new CheckEligibilityBulkResponse()
            {
                Data = response as List<CheckEligibilityItem>
            });
            
            return useCaseExecutionResult;
        }
    }
}