using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Authorization;
using CheckYourEligibility.API.Extensions;
using CheckYourEligibility.API.Gateways.Interfaces;
using CheckYourEligibility.API.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.API.Controllers
{
	[ApiController]
	[Route("[controller]")]
	[Authorize]
	public class TemporaryCodesController : BaseController
	{
		private readonly ILogger<TemporaryCodesController> _logger;
		private readonly ITemporaryCodeUploadUseCase _temporaryCodeUploadUseCase;

		public TemporaryCodesController(
			ILogger<TemporaryCodesController> logger,
			IAudit audit,
			IConfiguration configuration,
			ITemporaryCodeUploadUseCase temporaryCodeUploadUseCase
		)
			: base(audit)
		{
			_logger = logger;

			_temporaryCodeUploadUseCase = temporaryCodeUploadUseCase;
		}

		/// <summary>
		/// Bulk imports Temporary codes from an Excel file.
		/// </summary>
		/// <param name="request">The bulk import request containing binary file</param>
		/// <returns>Import results with success/failure counts and error details</returns>
		[ProducesResponseType(typeof(TemporaryCodeUploadCreateResponse), (int)HttpStatusCode.OK)]
		[ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
		[Consumes("multipart/form-data")]
		[HttpPost("/temporary-codes/upload")]
		[Authorize(Policy = PolicyNames.RequireAdminScope)]
		public async Task<ActionResult> TemporaryCodesUpload([FromForm] TemporaryCodeUploadCreateRequest request)
		{
			try
			{
				var response = await _importApplicationsUseCase.Execute(request);
				return Ok(response);
			}
			catch (ValidationException ex)
			{
				return BadRequest(new ErrorResponse { Errors = [new Error { Title = ex.Message }] });
			}
			catch (UnauthorizedAccessException ex)
			{
				return BadRequest(new ErrorResponse { Errors = [new Error { Title = ex.Message }] });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during bulk import");
				return BadRequest(new ErrorResponse { Errors = [new Error { Title = ex.Message }] });
			}
		}

	}
}
