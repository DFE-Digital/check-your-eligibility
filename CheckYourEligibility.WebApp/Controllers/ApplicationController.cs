using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ApplicationController : BaseController
    {
        private readonly ICreateApplicationUseCase _createApplicationUseCase;
        private readonly IGetApplicationUseCase _getApplicationUseCase;
        private readonly ISearchApplicationsUseCase _searchApplicationsUseCase;
        private readonly IUpdateApplicationStatusUseCase _updateApplicationStatusUseCase;
        private readonly ILogger<EligibilityCheckController> _logger;

        public ApplicationController(
            ILogger<EligibilityCheckController> logger,
            ICreateApplicationUseCase createApplicationUseCase,
            IGetApplicationUseCase getApplicationUseCase,
            ISearchApplicationsUseCase searchApplicationsUseCase,
            IUpdateApplicationStatusUseCase updateApplicationStatusUseCase,
            IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _createApplicationUseCase = Guard.Against.Null(createApplicationUseCase);
            _getApplicationUseCase = Guard.Against.Null(getApplicationUseCase);
            _searchApplicationsUseCase = Guard.Against.Null(searchApplicationsUseCase);
            _updateApplicationStatusUseCase = Guard.Against.Null(updateApplicationStatusUseCase);
        }

        /// <summary>
        /// Posts an application for FreeSchoolMeals
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationSaveItemResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost()]
        public async Task<ActionResult> Application([FromBody] ApplicationRequest model)
        {
            try
            {
                var response = await _createApplicationUseCase.Execute(model);
                return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
            }
            catch (ValidationException ex)
            {
                return BadRequest(new MessageResponse { Data = ex.Message });
            }
        }

        /// <summary>
        /// Gets an application by guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationItemResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}")]
        public async Task<ActionResult> Application(string guid)
        {
            var response = await _getApplicationUseCase.Execute(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Searches for applications based on the supplied filter
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationSearchResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [HttpPost("Search")]
        public async Task<ActionResult> ApplicationSearch([FromBody] ApplicationRequestSearch model)
        {
            var response = await _searchApplicationsUseCase.Execute(model);
            if (response == null)
            {
                return NoContent();
            }
            return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Updates the status of an application
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationStatusUpdateResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPatch("{guid}")]
        public async Task<ActionResult> ApplicationStatusUpdate(string guid, [FromBody] ApplicationStatusUpdateRequest model)
        {
            var response = await _updateApplicationStatusUseCase.Execute(guid, model);
            if (response == null)
            {
                return NotFound();
            }
            return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };
        }
    }
}