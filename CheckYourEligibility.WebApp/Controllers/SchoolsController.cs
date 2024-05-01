using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
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
                return BadRequest(new MessageResponse { Data = "At least 3 characters are required to query." });
            }

            var results = await _service.Search(query);
            if (results == null || !results.Any())
                return new ObjectResult(new SchoolSearchResponse { Data = results }) { StatusCode = StatusCodes.Status404NotFound };
            else
            {
                return new ObjectResult(new SchoolSearchResponse { Data = results }) { StatusCode = StatusCodes.Status200OK };
            }
        }
    }
}
