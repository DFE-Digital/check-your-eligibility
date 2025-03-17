// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CheckYourEligibility.WebApp.Controllers
{
    /// <summary>
    /// Administration Controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AdministrationController : BaseController
    {
        private readonly ICleanUpEligibilityChecksUseCase _cleanUpEligibilityChecksUseCase;
        private readonly IImportEstablishmentsUseCase _importEstablishmentsUseCase;
        private readonly IImportFsmHomeOfficeDataUseCase _importFsmHomeOfficeDataUseCase;
        private readonly IImportFsmHMRCDataUseCase _importFsmHMRCDataUseCase;

        /// <summary>
        /// Constructor for AdministrationController
        /// </summary>
        /// <param name="cleanUpEligibilityChecksUseCase"></param>
        /// <param name="importEstablishmentsUseCase"></param>
        /// <param name="importFsmHomeOfficeDataUseCase"></param>
        /// <param name="importFsmHMRCDataUseCase"></param>
        /// <param name="audit"></param>
        public AdministrationController(
            ICleanUpEligibilityChecksUseCase cleanUpEligibilityChecksUseCase,
            IImportEstablishmentsUseCase importEstablishmentsUseCase,
            IImportFsmHomeOfficeDataUseCase importFsmHomeOfficeDataUseCase,
            IImportFsmHMRCDataUseCase importFsmHMRCDataUseCase,
            IAudit audit) : base(audit)
        {
            _cleanUpEligibilityChecksUseCase = Guard.Against.Null(cleanUpEligibilityChecksUseCase);
            _importEstablishmentsUseCase = Guard.Against.Null(importEstablishmentsUseCase);
            _importFsmHomeOfficeDataUseCase = Guard.Against.Null(importFsmHomeOfficeDataUseCase);
            _importFsmHMRCDataUseCase = Guard.Against.Null(importFsmHMRCDataUseCase);
        }

        /// <summary>
        /// Deletes all old Eligibility Checks based on the service configuration
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        [HttpPut("/cleanUpEligibilityChecks")]
        [HttpPut("/admin/clean-up-eligibility-checks")]
        [Authorize(Policy = PolicyNames.RequireAdminScope)]
        public async Task<ActionResult> CleanUpEligibilityChecks()
        {
            await _cleanUpEligibilityChecksUseCase.Execute();
            return new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Imports Establishments
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("multipart/form-data")]
        [HttpPost("/importEstablishments")]
        [HttpPost("/admin/import-establishments")]
        [Authorize(Policy = PolicyNames.RequireAdminScope)]
        public async Task<ActionResult> ImportEstablishments(IFormFile file)
        {
            try
            {
                await _importEstablishmentsUseCase.Execute(file);
                return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.EstablishmentFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = ex.Message }]});
            }
        }

        /// <summary>
        /// Truncates FsmHomeOfficeData and imports a new data set from CSV input
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("multipart/form-data")]
        [HttpPost("/importFsmHomeOfficeData")]
        [HttpPost("/admin/import-fsm-home-office-data")]
        [Authorize(Policy = PolicyNames.RequireAdminScope)]
        public async Task<ActionResult> ImportFsmHomeOfficeData(IFormFile file)
        {
            try
            {
                await _importFsmHomeOfficeDataUseCase.Execute(file);
                return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HomeOfficeFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = ex.Message }]});
            }
        }

        /// <summary>
        /// Truncates FsmHMRCData and imports a new data set from XML input
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("multipart/form-data")]
        [HttpPost("/importFsmHMRCData")]
        [HttpPost("/admin/import-fsm-hmrc-data")]
        [Authorize(Policy = PolicyNames.RequireAdminScope)]
        public async Task<ActionResult> ImportFsmHMRCData(IFormFile file)
        {
            try
            {
                await _importFsmHMRCDataUseCase.Execute(file);
                return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HMRCFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = ex.Message }]});
            }
        }
    }
}