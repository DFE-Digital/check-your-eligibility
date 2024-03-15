using Ardalis.GuardClauses;
using Azure;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Support;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
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
            return new ObjectResult(ResponseFormatter.GetResponseMessage($"{Admin.EligibilityChecksCleanse}")) { StatusCode = StatusCodes.Status200OK };
        }

        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPost("/importEstablishments")]
        public async Task<ActionResult> ImportEstablishments(IFormFile file)
        {
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest("Csv data file is required."));
            }
            try
            {
                await _service.ImportEstablishments(file);
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportEstablishments", ex);
                return new ObjectResult(ResponseFormatter.GetResponseMessage($"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message},{ex.InnerException.Message}")) { StatusCode = StatusCodes.Status500InternalServerError }; 
            }
            
            return new ObjectResult(ResponseFormatter.GetResponseMessage($"{file.FileName} - {Admin.EstablishmentFileProcessed}")) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
