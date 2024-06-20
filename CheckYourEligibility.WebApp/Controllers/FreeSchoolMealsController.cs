using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using CheckEligibilityStatusResponse = CheckYourEligibility.Domain.Responses.CheckEligibilityStatusResponse;
using StatusValue = CheckYourEligibility.Domain.Responses.StatusValue;

namespace CheckYourEligibility.WebApp.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class FreeSchoolMealsController : BaseController
    {
        private readonly IFsmCheckEligibility _checkService;
        private readonly IFsmApplication _applicationService;
        
        private readonly ILogger<FreeSchoolMealsController> _logger;


        public FreeSchoolMealsController(ILogger<FreeSchoolMealsController> logger, IFsmCheckEligibility checkService, IFsmApplication applicationService, IAudit audit)
            : base( audit)
        {
            _logger = Guard.Against.Null(logger);
            _checkService = Guard.Against.Null(checkService);
            _applicationService = Guard.Against.Null(applicationService);
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
            var validationResults = validator.Validate(model.Data);

            if (!validationResults.IsValid)
            {
                return BadRequest(new MessageResponse { Data = validationResults.ToString() });
            }

            var response = await _checkService.PostCheck(model.Data);

            await AuditAdd(Domain.Enums.AuditType.Check, response.Id);

            return new ObjectResult(new CheckEligibilityResponse()
            {
                Data = new StatusValue() { Status = response.Status.ToString() },
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{Domain.Constants.FSMLinks.GetLink}{response.Id}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.FSMLinks.ProcessLink}{response.Id}",
                    Get_EligibilityCheckStatus = $"{Domain.Constants.FSMLinks.GetLink}{response.Id}/Status"
                }
            })
            { StatusCode = StatusCodes.Status202Accepted };
        }


       /// <summary>
       /// Posts the array of checks
       /// </summary>
       /// <param name="model"></param>
       /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost("Bulk")]
        public async Task<ActionResult> CheckEligibilityBulk([FromBody] CheckEligibilityRequestBulk model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid CheckEligibilityRequest, data is required." });
            }

            var validator = new CheckEligibilityRequestDataValidator();
            var inc = 1;
            var validationResultsItems = new StringBuilder();
            foreach (var item in model.Data)
            {
                item.NationalInsuranceNumber = item.NationalInsuranceNumber?.ToUpper();
                item.NationalAsylumSeekerServiceNumber = item.NationalAsylumSeekerServiceNumber?.ToUpper();
                var validationResults = validator.Validate(item);
                if (!validationResults.IsValid)
                {
                    validationResultsItems.AppendLine($"Item:-{inc}, {validationResults.ToString()}");
                }
            }
            

            if (validationResultsItems.Length > 0 )
            {
                return BadRequest(new MessageResponse { Data = validationResultsItems.ToString() });
            }

            var groupId = Guid.NewGuid().ToString();
            await _checkService.PostCheck(model.Data, groupId);
            await AuditAdd(Domain.Enums.AuditType.BulkCheck, groupId);

            return new ObjectResult(new CheckEligibilityResponseBulk()
            {
                Data = new StatusValue() { Status = $"{Domain.Constants.Messages.Processing}" },
                Links = new CheckEligibilityResponseBulkLinks
                {
                    Get_Progress_Check = $"{Domain.Constants.FSMLinks.BulkCheckLink}{groupId}{Domain.Constants.FSMLinks.BulkCheckProgress}",
                    Get_BulkCheck_Results = $"{Domain.Constants.FSMLinks.BulkCheckLink}{groupId}{Domain.Constants.FSMLinks.BulkCheckResults}"
                }
            })
            { StatusCode = StatusCodes.Status202Accepted };
        }

        /// <summary>
        /// Bulk Upload status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityBulkStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("Bulk/{guid}/CheckProgress")]
        public async Task<ActionResult> BulkUploadProgress(string guid)
        {
            var response = await _checkService.GetBulkStatus(guid);
            if (response == null)
            {
                return NotFound(guid);
            }

            return new ObjectResult(new CheckEligibilityBulkStatusResponse()
            {
                Data = response,
                Links = new BulkCheckResponseLinks()
                { Get_BulkCheck_Results = $"{Domain.Constants.FSMLinks.BulkCheckLink}{guid}{Domain.Constants.FSMLinks.BulkCheckResults}" }
            })
            { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Loads results of bulk loads given a group Id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityBulkResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("Bulk/{guid}/Results")]
        public async Task<ActionResult> BulkUploadResults(string guid)
        {
            var response = await _checkService.GetBulkCheckResults(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            await AuditAdd(Domain.Enums.AuditType.CheckBulkResults, guid);
            return new ObjectResult(new CheckEligibilityBulkResponse()
            {
                Data = response
            })
            { StatusCode = StatusCodes.Status200OK };
        }


        /// <summary>
        /// Gets an FSM an Eligibility Check status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}/Status")]
        public async Task<ActionResult> CheckEligibilityStatus(string guid)
        {
            var response = await _checkService.GetStatus(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            await AuditAdd(Domain.Enums.AuditType.Check, guid);

            return new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status200OK };
        }


        

        /// <summary>
        /// Updates an Eligibility check status
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPatch("{guid}/Status")]
        public async Task<ActionResult> EligibilityCheckStatusUpdate(string guid, [FromBody] EligibilityStatusUpdateRequest model)
        {
            var response = await _checkService.UpdateEligibilityCheckStatus(guid, model.Data);
            if (response == null)
            {
                return NotFound();
            }

            await AuditAdd(Domain.Enums.AuditType.Check, guid);

            return new ObjectResult(new CheckEligibilityStatusResponse
            {
                Data = response.Data
            })
            { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Processes FSM an Eligibility Check producing an outcome status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <remarks>If a dependent service, ie DWP fails then the status is not updated</remarks>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPut("ProcessEligibilityCheck/{guid}")]
        public async Task<ActionResult> Process(string guid)
        {
            try
            {
                var auditItemTemplate = AuditDataGet(Domain.Enums.AuditType.Check, string.Empty);
                var response = await _checkService.ProcessCheck(guid, auditItemTemplate);
                if (response == null)
                {
                    return NotFound(guid);
                }

                await AuditAdd(Domain.Enums.AuditType.Check, guid);

                if (response.Value == Domain.Enums.CheckEligibilityStatus.queuedForProcessing)
                {
                    return new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                }
                else
                {
                    return new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status200OK };
                }
                
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
            var response = await _checkService.GetItem(guid);
            if (response == null)
            {
                return NotFound(guid);
            }
            await AuditAdd(Domain.Enums.AuditType.Check, guid);
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

            var response = await _applicationService.PostApplication(model.Data);
            await AuditAdd(Domain.Enums.AuditType.Application, response.Id);

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
            var response = await _applicationService.GetApplications(model.Data);
            if (response == null | !response.Any())
            {
                return NoContent();
            }
            await AuditAdd(Domain.Enums.AuditType.Application, string.Empty);
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
