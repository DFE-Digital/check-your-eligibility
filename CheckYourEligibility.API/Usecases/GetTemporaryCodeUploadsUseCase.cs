using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Gateways.Interfaces;

namespace CheckYourEligibility.API.Usecases
{
	public interface IGetTemporaryCodeUploadsUseCase
	{
		Task<TemporaryCodeUploadSearchResponse> Execute(TemporaryCodeUploadsSearchRequest request);
	}

	public class GetTemporaryCodeUploadsUseCase : IGetTemporaryCodeUploadsUseCase
	{
		private readonly ITemporaryCodeUpload _gateway;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<GetTemporaryCodeUploadsUseCase> _logger;

		public GetTemporaryCodeUploadsUseCase(
		ITemporaryCodeUpload gateway,
		IServiceScopeFactory scopeFactory,
		ILogger<GetTemporaryCodeUploadsUseCase> logger)
		{
			_gateway = gateway;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task<TemporaryCodeUploadSearchResponse> Execute(TemporaryCodeUploadsSearchRequest request)
		{
			var response = await _gateway.GetTemporaryCodeUploads(request);

			return new TemporaryCodeUploadSearchResponse
			{
				PageNumber = response.PageNumber,
				PageSize = response.PageSize,
				TotalNumberOfRecords = response.TotalNumberOfRecords,
				Data = response.Data
			};
		}
	}
}
