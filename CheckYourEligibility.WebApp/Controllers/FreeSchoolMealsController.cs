using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.Support;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FreeSchoolMealsController : Controller
    {
        private readonly ILogger<FreeSchoolMealsController> _logger;
        private readonly IFsmCheckEligibility _service;

        public FreeSchoolMealsController(ILogger<FreeSchoolMealsController> logger, IFsmCheckEligibility service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        [ProducesResponseType(typeof(Response), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest("Invalid CheckEligibilityRequest, data is required."));
            }
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber?.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest(validationResults.ToString()));
            }

            var id = await _service.PostCheck(model.Data);
            return new ObjectResult(ResponseFormatter.GetResponseStatus(Domain.Enums.CheckEligibilityStatus.queuedForProcessing, id)) { StatusCode = StatusCodes.Status202Accepted };
        }

        [ProducesResponseType(typeof(CheckEligibilityStatus), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}/Status")]
        public async Task<ActionResult> CheckEligibilityStatus(string guid)
        {
            var response = await _service.GetStatus(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            return new ObjectResult(ResponseFormatter.GetResponseStatus(response)) { StatusCode = StatusCodes.Status200OK };
        }


        [ProducesResponseType(typeof(Response), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPut("ProcessEligibilityCheck/{guid}")]
        public async Task<ActionResult> Process(string guid)
        {
            var response = await _service.ProcessCheck(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            return new ObjectResult(ResponseFormatter.GetResponseStatus(response,guid)) { StatusCode = StatusCodes.Status200OK };
        }

        [ProducesResponseType(typeof(CheckEligibilityItemFsm), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}")]
        public async Task<ActionResult> EligibilityCheck(string guid)
        {
            var response = await _service.GetItem(guid);
            if (response == null)
            {
                return NotFound(guid);
            }

            return new ObjectResult(ResponseFormatter.GetResponseItem(response)) { StatusCode = StatusCodes.Status200OK };
        }

        [ProducesResponseType(typeof(Response), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost("Application")]
        public async Task<ActionResult> Application([FromBody] ApplicationRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest("Invalid request, data is required."));
            }
            model.Data.ParentNationalInsuranceNumber = model.Data.ParentNationalInsuranceNumber?.ToUpper();
            model.Data.ParentNationalAsylumSeekerServiceNumber = model.Data.ParentNationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new ApplicationRequestFsmValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(ResponseFormatter.GetResponseBadRequest(validationResults.ToString()));
            }

            var response = await _service.PostApplication(model.Data);
            return new ObjectResult(ResponseFormatter.GetResponseApplication(response)) { StatusCode = StatusCodes.Status201Created };
        }

        [ProducesResponseType(typeof(ApplicationFsm), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("Application/{guid}")]
        public async Task<ActionResult> Application(string guid)
        {
            var response = await _service.GetApplication(guid);
            if (response == null)
            {
                return NotFound(guid);
            }

            return new ObjectResult(ResponseFormatter.GetResponseApplication(response)) { StatusCode = StatusCodes.Status200OK };
        }

        [ProducesResponseType(typeof(IEnumerable<ApplicationFsm>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [HttpPost("Application/Search")]
        public async Task<ActionResult> ApplicationSearch([FromBody] ApplicationRequestSearch model)
        {
            var response = await _service.GetApplications(model.Data);
            if (response == null | !response.Any())
            {
                return NoContent();
            }

            return new ObjectResult(ResponseFormatter.GetResponseApplication(response)) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
