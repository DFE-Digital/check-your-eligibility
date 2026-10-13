using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Gateways.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CheckYourEligibility.API.Gateways;

public sealed class TemporaryCodeUploadGateway : ITemporaryCodeUpload
{
	private readonly IEligibilityCheckContext _db;
	private readonly ILogger<TemporaryCodeUploadGateway> _logger;

	public TemporaryCodeUploadGateway(
		IEligibilityCheckContext db,
		ILogger<TemporaryCodeUploadGateway> logger)
	{
		_db = db;
		_logger = logger;
	}

	/// <summary>
	/// Receives a TemporaryCodeUploadCreateRequest and creates a new TemporaryCodeUpload record in the database.
	/// </summary>
	/// <param name="request"></param>
	/// <returns>TemporaryCodeUpload</returns>
	//CREATE
	public async Task<TemporaryCodeUpload> CreateTemporaryCodeUpload(TemporaryCodeUploadCreateRequest request, Guid userID, int numberOfCodes, string firstCode, string lastCode)
	{
		var temporaryCodeUpload = new TemporaryCodeUpload
		{
			TemporaryCodeUploadId = Guid.NewGuid(),
			Filename = request.File.FileName,
			NumberOfCodes = numberOfCodes,
			FirstCode = firstCode,
			LastCode = lastCode,
			DateImported = DateTime.UtcNow,
			ImportedBy = userID,
			IsDeleted = false
		};

		_db.TemporaryCodeUploads.Add(temporaryCodeUpload);
		await _db.SaveChangesAsync();

		return temporaryCodeUpload;
	}

	/// <summary>
	/// Returns a paginated list of TemporaryCodeUpload records based on the provided search request.
	/// </summary>
	/// <param name="request"></param>
	/// <returns>TemporaryCodeUploadSearchResponse</returns>
	//SEARCH
	public async Task<TemporaryCodeUploadSearchResponse> GetTemporaryCodeUploads(TemporaryCodeUploadsSearchRequest request)
	{
		const int pageSize = 10;

		try
		{
			// Base query to get non-deleted temoraryCodeUploads
			var query = _db.TemporaryCodeUploads
				.Where(r => !r.IsDeleted);

			// Get total records
			var totalRecords = await query.CountAsync();

			// Ensure page number is valid
			var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

			// Calculate total pages and adjust page number if it exceeds max
			var maxPage = totalRecords == 0
				? 1
				: (int)Math.Ceiling(totalRecords / (double)pageSize);

			// If requested page number exceeds max, return the last page
			if (pageNumber > maxPage)
			{
				pageNumber = maxPage;
			}

			// Retrieve paginated results
			var cutoffDate = DateTime.UtcNow.Date.AddDays(-30);
			var temporaryCodeUploadItems = await query
				.Where(r => r.DateImported >= cutoffDate)
				.OrderByDescending(r => r.DateImported)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(r => new TemporaryCodeUpload
				{
					TemporaryCodeUploadId = r.TemporaryCodeUploadId,
					Filename = r.Filename,
					NumberOfCodes = r.NumberOfCodes,
					FirstCode = r.FirstCode,
					LastCode = r.LastCode,
					DateImported = r.DateImported,
					ImportedBy = r.ImportedBy,
					IsDeleted = r.IsDeleted
				})
				.ToListAsync();

			return new TemporaryCodeUploadSearchResponse
			{
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalNumberOfRecords = totalRecords,
				Data = (IEnumerable<TemporaryCodeUpload>)temporaryCodeUploadItems,
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving eligibility check report history");
			throw;
		}
	}
}