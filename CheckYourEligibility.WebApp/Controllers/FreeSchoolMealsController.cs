using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using StatusResponse = CheckYourEligibility.Domain.Responses.StatusResponse;
using StatusValue = CheckYourEligibility.Domain.Responses.StatusValue;

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

        /// <summary>
        /// Posts a FSM Eligibility Check to the processing queue
        /// </summary>
        /// <param name="CheckEligibilityRequest"></param>
        /// <remarks>If the check has already been submitted, then the stored Hash is returned</remarks>
        /// <links cref="https://stackoverflow.com/questions/61896978/asp-net-core-swaggerresponseexample-not-outputting-specified-example"/>
        [ProducesResponseType(typeof(CheckEligibilityResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid CheckEligibilityRequest, data is required." });
            }
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber?.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(new MessageResponse { Data = validationResults.ToString() });
            }
            var response = await _service.PostCheck(model.Data);
            return new ObjectResult(new CheckEligibilityResponse() { 
                Data = new StatusValue() { Status = response.Status.ToString() },
                Links = new CheckEligibilityResponseLinks {
                    Get_EligibilityCheck = $"{Domain.Constants.FSMLinks.GetLink}{response.Id}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.FSMLinks.ProcessLink}{response.Id}",
                    Get_EligibilityCheckStatus = $"{Domain.Constants.FSMLinks.GetLink}{response.Id}/Status"
                }
            }) { StatusCode = StatusCodes.Status202Accepted };
        }

        /// <summary>
        /// Gets an FSM an Eligibility Check status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(StatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}/Status")]
        public async Task<ActionResult> CheckEligibilityStatus(string guid)
        {
            var response = await _service.GetStatus(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            return new ObjectResult(new StatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Processes FSM an Eligibility Check producing an outcome status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(StatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPut("ProcessEligibilityCheck/{guid}")]
        public async Task<ActionResult> Process(string guid)
        {
            try
            {
                var response = await _service.ProcessCheck(guid);
                if (response == null)
                {
                    return NotFound(guid);
                }
                return new ObjectResult(new StatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (ProcessCheckException)
            {
                return BadRequest(guid);
            }
        }

        /// <summary>
        /// Gets an Eligibility check using the supplied GUID 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityItemResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}")]
        public async Task<ActionResult> EligibilityCheck(string guid)
        {
            var response = await _service.GetItem(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            return new ObjectResult(new CheckEligibilityItemResponse()
            {
                Data = response,
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{Domain.Constants.FSMLinks.GetLink}{guid}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.FSMLinks.ProcessLink}{guid}"
                }
            })
            { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Posts an application
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationSaveItemResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost("Application")]
        public async Task<ActionResult> Application([FromBody] ApplicationRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid request, data is required." });
            }
            model.Data.ParentNationalInsuranceNumber = model.Data.ParentNationalInsuranceNumber?.ToUpper();
            model.Data.ParentNationalAsylumSeekerServiceNumber = model.Data.ParentNationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new ApplicationRequestValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(new MessageResponse { Data = validationResults.ToString() });
            }

            var response = await _service.PostApplication(model.Data);

            return new ObjectResult(new ApplicationSaveItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{response.Id}"
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
        [HttpGet("Application/{guid}")]
        public async Task<ActionResult> Application(string guid)
        {
            var response = await _service.GetApplication(guid);
            if (response == null)
            {
                return NotFound(guid);
            }

            return new ObjectResult(new ApplicationItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.FSMLinks.GetLinkApplication}{response.Id}"
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
        [HttpPost("Application/Search")]
        public async Task<ActionResult> ApplicationSearch([FromBody] ApplicationRequestSearch model)
        {
            var response = await _service.GetApplications(model.Data);
            if (response == null | !response.Any())
            {
                return NoContent();
            }

            return new ObjectResult(new ApplicationSearchResponse
            {
                Data = response
            })
            { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Updates an application status
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApplicationStatusUpdateResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPatch("Application/{guid}")]
        public async Task<ActionResult> ApplicationStatusUpdate(string guid, [FromBody] ApplicationStatusUpdateRequest model)
        {
            var response = await _service.UpdateApplicationStatus(guid, model.Data);
            if (response == null)
            {
                return NotFound();
            }

            return new ObjectResult(new ApplicationStatusUpdateResponse
            {
                Data = response.Data
            })
            { StatusCode = StatusCodes.Status200OK };
        }
    }
 }
