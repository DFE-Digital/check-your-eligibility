using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CheckYourEligibility.Domain.Exceptions;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for retrieving bulk upload progress status
    /// </summary>
    public interface IGetBulkUploadProgressUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="guid">The group ID of the bulk upload</param>
        /// <returns>Bulk upload progress status</returns>
        Task<CheckEligibilityBulkStatusResponse> Execute(string guid);
    }

    public class GetBulkUploadProgressUseCase : IGetBulkUploadProgressUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly ILogger<GetBulkUploadProgressUseCase> _logger;

        public GetBulkUploadProgressUseCase(
            ICheckEligibility checkService,
            ILogger<GetBulkUploadProgressUseCase> logger)
        {
            _checkService = checkService;
            _logger = logger;
        }

        public async Task<CheckEligibilityBulkStatusResponse> Execute(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ValidationException(null, "Invalid Request, group ID is required.");
            }

            var response = await _checkService.GetBulkStatus(guid);
            if (response == null)
            {
                _logger.LogWarning($"Bulk upload with ID {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} not found");
                throw new NotFoundException(guid.ToString());
            }

            _logger.LogInformation($"Retrieved bulk upload progress for group ID: {guid.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")}");
            
            return new CheckEligibilityBulkStatusResponse()
            {
                Data = response,
                Links = new BulkCheckResponseLinks()
                { 
                    Get_BulkCheck_Results = $"{CheckLinks.BulkCheckLink}{guid}{CheckLinks.BulkCheckResults}" 
                }
            };
        }
    }
}