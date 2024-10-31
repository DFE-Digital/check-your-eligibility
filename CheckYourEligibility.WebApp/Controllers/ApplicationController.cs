using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    //EligibilityController

    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ApplicationController : BaseController
    {
        private readonly IApplication _applicationService;
        private readonly ILogger<EligibilityCheckController> _logger;


        public ApplicationController(ILogger<EligibilityCheckController> logger, IApplication applicationService, IAudit audit)
            : base( audit)
        {
            _logger = Guard.Against.Null(logger);
            _applicationService = Guard.Against.Null(applicationService);
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
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid request, data is required" });
            }
            if (model.Data.Type == Domain.Enums.CheckEligibilityType.None)
            {
                return BadRequest(new MessageResponse { Data = $"Invalid request, Valid Type is required{model.Data.Type}" });
            }
            model.Data.ParentNationalInsuranceNumber = model.Data.ParentNationalInsuranceNumber?.ToUpper();
            model.Data.ParentNationalAsylumSeekerServiceNumber = model.Data.ParentNationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new ApplicationRequestValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(new MessageResponse { Data = validationResults.ToString() });
            }
            var response = await _applicationService.PostApplication(model.Data);
            await AuditAdd(Domain.Enums.AuditType.Application, response.Id);

            return new ObjectResult(new ApplicationSaveItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{response.Id}"
                }
            })
            { StatusCode = StatusCodes.Status201Created };

        }

        /// <summary>
        /// Gets an Application
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationItemResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}")]
        public async Task<ActionResult> Application(string guid)
        {
            var response = await _applicationService.GetApplication(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            await AuditAdd(Domain.Enums.AuditType.Application, guid);
            return new ObjectResult(new ApplicationItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{response.Id}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };
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
            var response = await _applicationService.GetApplications(model);

            if (response == null || !response.Data.Any())
            {
                return NoContent();
            }

            await AuditAdd(Domain.Enums.AuditType.Application, string.Empty);

            return new ObjectResult(response)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Updates an application status
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationStatusUpdateResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPatch("{guid}")]
        public async Task<ActionResult> ApplicationStatusUpdate(string guid, [FromBody] ApplicationStatusUpdateRequest model)
        {

            var response = await _applicationService.UpdateApplicationStatus(guid, model.Data);
            if (response == null)
            {
                return NotFound();
            }
            await AuditAdd(Domain.Enums.AuditType.Application, guid);
            return new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = response.Data
            })
            { StatusCode = StatusCodes.Status200OK };

        }

    }
 }
