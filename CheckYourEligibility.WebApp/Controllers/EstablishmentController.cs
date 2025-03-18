using Ardalis.GuardClauses;
using Azure;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class EstablishmentController : BaseController
    {
        private readonly ILogger<EstablishmentController> _logger;
        private readonly ISearchEstablishmentsUseCase _searchUseCase;

        public EstablishmentController(
            ILogger<EstablishmentController> logger,
            ISearchEstablishmentsUseCase searchUseCase,
            IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _searchUseCase = Guard.Against.Null(searchUseCase);
        }

        [ProducesResponseType(typeof(IEnumerable<Establishment>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpGet("/establishment/search")]
        [HttpGet("/Establishments/Search")]
        [Authorize(Policy = PolicyNames.RequireEstablishmentScope)]
        public async Task<ActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = "At least 3 characters are required to query." }]});
            }

            var results = await _searchUseCase.Execute(query);

            if (!results.Any())
            {
                return new ObjectResult(new EstablishmentSearchResponse { Data = results }) { StatusCode = StatusCodes.Status404NotFound };
            }
            else
            {
                return new ObjectResult(new EstablishmentSearchResponse { Data = results }) { StatusCode = StatusCodes.Status200OK };
            }
        }
    }
}
