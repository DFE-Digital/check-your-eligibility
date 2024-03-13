using Ardalis.GuardClauses;
using Azure;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
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
    public class SchoolsController : Controller
    {
        private readonly ILogger<SchoolsController> _logger;
        private readonly ISchoolsSearch _service;

        public SchoolsController(ILogger<SchoolsController> logger, ISchoolsSearch service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        [ProducesResponseType(typeof(IEnumerable<School>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpGet("search")]
        public async Task<ActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest("At least 3 characters are required to query."));
            }

            var results = await _service.Search(query);
            if (results == null || !results.Any())
                return new ObjectResult(ResponseFormatter.GetSchoolsResponseMessage(results)) { StatusCode = StatusCodes.Status404NotFound };
            else
            {
                return new ObjectResult(ResponseFormatter.GetSchoolsResponseMessage(results)) { StatusCode = StatusCodes.Status200OK };
            }
        }
    }
}
