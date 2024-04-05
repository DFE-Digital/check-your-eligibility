using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdministrationController : Controller
    {
        private readonly ILogger<AdministrationController> _logger;
        private readonly IAdministration _service;

        public AdministrationController(ILogger<AdministrationController> logger, IAdministration service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        /// <summary>
        /// Deletes all old Eligibility Checks based on the service configuration
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPut("/cleanUpEligibilityChecks")]
        public async Task<ActionResult> CleanUpEligibilityChecks()
        {
            await _service.CleanUpEligibilityChecks();
            return new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };
        }

        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPost("/importEstablishments")]
        public async Task<ActionResult> ImportEstablishments(IFormFile file)
        {
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                return BadRequest(new MessageResponse { Data = "Csv data file is required." });
            }
            try
            {
                await _service.ImportEstablishments(file);
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportEstablishments", ex);
                return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message},{ex.InnerException.Message}" }) { StatusCode = StatusCodes.Status500InternalServerError };
            }

            return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.EstablishmentFileProcessed}"}){ StatusCode = StatusCodes.Status200OK };
        }
    }
}
