using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for processing messages from a specified queue
    /// </summary>
    public interface IProcessQueueMessagesUseCase
    {
        /// <summary>
        /// Execute the use case
        /// </summary>
        /// <param name="queue">Queue identifier</param>
        /// <returns>A message response indicating success</returns>
        Task<MessageResponse> Execute(string queue);
    }

    public class ProcessQueueMessagesUseCase : IProcessQueueMessagesUseCase
    {
        private readonly ICheckEligibility _checkService;
        private readonly ILogger<ProcessQueueMessagesUseCase> _logger;

        public ProcessQueueMessagesUseCase(ICheckEligibility checkService, ILogger<ProcessQueueMessagesUseCase> logger)
        {
            _checkService = Guard.Against.Null(checkService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task<MessageResponse> Execute(string queue)
        {
            if (string.IsNullOrEmpty(queue))
            {
                _logger.LogWarning("Empty queue name provided to ProcessQueueMessagesUseCase");
                return new MessageResponse { Data = "Invalid Request." };
            }
            
            await _checkService.ProcessQueue(queue);
            _logger.LogInformation($"Queue {queue.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} processed successfully");
            return new MessageResponse { Data = "Queue Processed." };
        }
    }
}