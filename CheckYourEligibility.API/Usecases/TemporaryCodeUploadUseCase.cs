using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;

namespace CheckYourEligibility.API.Usecases
{
	public interface ITemporaryCodeUploadUseCase
	{
		Task<TemporaryCodeUploadCreateResponse> Execute(TemporaryCodeUploadCreateRequest request, Guid userId);
	}

	public class TemporaryCodeUploadUseCase : ITemporaryCodeUploadUseCase
	{
		private readonly ITemporaryCodeUpload _gateway;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<TemporaryCodeUploadUseCase> _logger;

		public TemporaryCodeUploadUseCase(
		ITemporaryCodeUpload gateway,
		IServiceScopeFactory scopeFactory,
		ILogger<TemporaryCodeUploadUseCase> logger)
		{
			_gateway = gateway;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task<TemporaryCodeUploadCreateResponse> Execute(TemporaryCodeUploadCreateRequest request, Guid userId)
		{
			//call ImportWfHMRCDataUseCase
			var response = await _gateway.CreateTemporaryCodeUpload(request, userId);

			return new TemporaryCodeUploadCreateResponse
			{
				Errors = ["Not implemented"]
			};
		}

	}
}
