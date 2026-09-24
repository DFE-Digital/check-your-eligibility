using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Domain.Exceptions;
using CheckYourEligibility.API.UseCases;
using FluentValidation;

namespace CheckYourEligibility.API.Usecases
{
	public interface ITemporaryCodeUploadUseCase
	{
		Task<TemporaryCodeUploadCreateResponse> Execute(TemporaryCodeUploadCreateRequest request);
	}

	public class TemporaryCodeUploadUseCase : ITemporaryCodeUploadUseCase
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<TemporaryCodeUploadUseCase> _logger;

		public TemporaryCodeUploadUseCase(
		IServiceScopeFactory scopeFactory,
		ILogger<TemporaryCodeUploadUseCase> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task<TemporaryCodeUploadCreateResponse> Execute(TemporaryCodeUploadCreateRequest request)
		{
			return new TemporaryCodeUploadCreateResponse
			{
				Errors = ["Not implemented"]
			};
		}

	}
}
