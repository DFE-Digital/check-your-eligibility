using Ardalis.GuardClauses;
using Azure;
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
    public class EstablishmentsController : BaseController
    {
        private readonly ILogger<EstablishmentsController> _logger;
        private readonly IEstablishmentSearch _service;

        public EstablishmentsController(ILogger<EstablishmentsController> logger, IEstablishmentSearch service, IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        [ProducesResponseType(typeof(IEnumerable<Establishment>), (int)HttpStatusCode.OK)]
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
            await AuditAdd(Domain.Enums.AuditType.Establishment, string.Empty);
            if (results == null || !results.Any())
                return new ObjectResult(new EstablishmentSearchResponse { Data = results }) { StatusCode = StatusCodes.Status404NotFound };
            else
            {
                return new ObjectResult(new EstablishmentSearchResponse { Data = results }) { StatusCode = StatusCodes.Status200OK };
            }
        }
    }
}
