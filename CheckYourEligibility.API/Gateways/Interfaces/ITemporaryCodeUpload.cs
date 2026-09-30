using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain;

namespace CheckYourEligibility.API.Gateways.Interfaces
{
	public interface ITemporaryCodeUpload
	{
		Task<TemporaryCodeUpload> CreateTemporaryCodeUpload(TemporaryCodeUploadCreateRequest request, CancellationToken cancellationToken);
		Task<TemporaryCodeUploadSearchResponse> GetTemporaryCodeUploads(TemporaryCodeUploadsSearchRequest request);
	}
}