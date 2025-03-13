using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
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
    public class EligibilityCheckController : BaseController
    {
        private readonly ILogger<EligibilityCheckController> _logger;
        private readonly int _bulkUploadRecordCountLimit;

        // Use case services
        private readonly IProcessQueueMessagesUseCase _processQueueMessagesUseCase;
        private readonly ICheckEligibilityForFSMUseCase _checkEligibilityForFsmUseCase;
        private readonly ICheckEligibilityBulkUseCase _checkEligibilityBulkUseCase;
        private readonly IGetBulkUploadProgressUseCase _getBulkUploadProgressUseCase;
        private readonly IGetBulkUploadResultsUseCase _getBulkUploadResultsUseCase;
        private readonly IGetEligibilityCheckStatusUseCase _getEligibilityCheckStatusUseCase;
        private readonly IUpdateEligibilityCheckStatusUseCase _updateEligibilityCheckStatusUseCase;
        private readonly IProcessEligibilityCheckUseCase _processEligibilityCheckUseCase;
        private readonly IGetEligibilityCheckItemUseCase _getEligibilityCheckItemUseCase;

        public EligibilityCheckController(
            ILogger<EligibilityCheckController> logger,
            IAudit audit,
            IConfiguration configuration,
            IProcessQueueMessagesUseCase processQueueMessagesUseCase,
            ICheckEligibilityForFSMUseCase checkEligibilityForFsmUseCase,
            ICheckEligibilityBulkUseCase checkEligibilityBulkUseCase,
            IGetBulkUploadProgressUseCase getBulkUploadProgressUseCase,
            IGetBulkUploadResultsUseCase getBulkUploadResultsUseCase,
            IGetEligibilityCheckStatusUseCase getEligibilityCheckStatusUseCase,
            IUpdateEligibilityCheckStatusUseCase updateEligibilityCheckStatusUseCase,
            IProcessEligibilityCheckUseCase processEligibilityCheckUseCase,
            IGetEligibilityCheckItemUseCase getEligibilityCheckItemUseCase)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _bulkUploadRecordCountLimit = configuration.GetValue<int>("BulkEligibilityCheckLimit");

            // Initialize use cases
            _processQueueMessagesUseCase = Guard.Against.Null(processQueueMessagesUseCase);
            _checkEligibilityForFsmUseCase = Guard.Against.Null(checkEligibilityForFsmUseCase);
            _checkEligibilityBulkUseCase = Guard.Against.Null(checkEligibilityBulkUseCase);
            _getBulkUploadProgressUseCase = Guard.Against.Null(getBulkUploadProgressUseCase);
            _getBulkUploadResultsUseCase = Guard.Against.Null(getBulkUploadResultsUseCase);
            _getEligibilityCheckStatusUseCase = Guard.Against.Null(getEligibilityCheckStatusUseCase);
            _updateEligibilityCheckStatusUseCase = Guard.Against.Null(updateEligibilityCheckStatusUseCase);
            _processEligibilityCheckUseCase = Guard.Against.Null(processEligibilityCheckUseCase);
            _getEligibilityCheckItemUseCase = Guard.Against.Null(getEligibilityCheckItemUseCase);
        }

        /// <summary>
        /// Processes check messages on the specified queue
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(MessageResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpPost("ProcessQueueMessages")]
        [HttpPost("/engine/process")]
        public async Task<ActionResult> ProcessQueue(string queue)
        {
            var result = await _processQueueMessagesUseCase.Execute(queue);

            if (result.Data == "Invalid Request.")
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.Data}]});
            }

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Posts a FSM Eligibility Check to the processing queue
        /// </summary>
        /// <param name="model"></param>
        /// <remarks>If the check has already been submitted, then the stored Hash is returned</remarks>
        [ProducesResponseType(typeof(CheckEligibilityResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpPost("FreeSchoolMeals")]
        [HttpPost("/check/free-school-meals")]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest_Fsm model)
        {
            var result = await _checkEligibilityForFsmUseCase.Execute(model);

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status202Accepted };
        }

        /// <summary>
        /// Posts the array of FSM checks
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityResponseBulk), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpPost("/EligibilityCheck/FreeSchoolMeals/Bulk")]
        [HttpPost("/bulk-check/free-school-meals")]
        public async Task<ActionResult> CheckEligibilityBulk([FromBody] CheckEligibilityRequestBulk_Fsm model)
        {
            var result = await _checkEligibilityBulkUseCase.Execute(model, _bulkUploadRecordCountLimit);

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status202Accepted };
        }

        /// <summary>
        /// Bulk Upload status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityBulkStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpGet("Bulk/{guid}/CheckProgress")]
        [HttpGet("/bulk-check/{guid}/progress")]
        public async Task<ActionResult> BulkUploadProgress(string guid)
        {
            var result = await _getBulkUploadProgressUseCase.Execute(guid);

            if (result.IsNotFound)
            {
                return NotFound(new ErrorResponse { Errors = [new Error() {Title = guid}]});
            }

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Loads results of bulk loads given a group Id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityBulkResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpGet("Bulk/{guid}/Results")]
        [HttpGet("/bulk-check/{guid}")]
        public async Task<ActionResult> BulkUploadResults(string guid)
        {
            var result = await _getBulkUploadResultsUseCase.Execute(guid);

            if (result.IsNotFound)
            {
                return NotFound(new ErrorResponse() { Errors = [new Error() { Title = guid, Status = "404" }] });
            }

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse() { Errors = [new Error() { Title = result.ValidationErrors, Status = "400" } ]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Gets an FSM an Eligibility Check status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpGet("{guid}/Status")]
        [HttpGet("/check/{guid}/status")]
        public async Task<ActionResult> CheckEligibilityStatus(string guid)
        {
            var result = await _getEligibilityCheckStatusUseCase.Execute(guid);

            if (result.IsNotFound)
            {
                return NotFound(new ErrorResponse { Errors = [new Error() {Title = guid}]});
            }

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Updates an Eligibility check status
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpPatch("{guid}/Status")]
        [HttpPatch("/engine/process/{guid}/status")]
        public async Task<ActionResult> EligibilityCheckStatusUpdate(string guid, [FromBody] EligibilityStatusUpdateRequest model)
        {
            var result = await _updateEligibilityCheckStatusUseCase.Execute(guid, model);

            if (result.IsNotFound)
            {
                return NotFound(new ErrorResponse { Errors = [new Error() {Title = ""}]});
            }

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Processes FSM an Eligibility Check producing an outcome status
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        /// <remarks>If a dependent service, ie DWP fails then the status is not updated</remarks>
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpPut("ProcessEligibilityCheck/{guid}")]
        [HttpPut("/engine/process/{guid}")]
        public async Task<ActionResult> Process(string guid)
        {
            try
            {
                var result = await _processEligibilityCheckUseCase.Execute(guid);

                if (result.IsNotFound)
                {
                    return NotFound(new ErrorResponse { Errors = [new Error() {Title = guid}]});
                }

                if (!result.IsValid && !result.IsServiceUnavailable)
                {
                    return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors }]});
                }

                if (result.IsServiceUnavailable)
                {
                    return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                }

                return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
            }
            catch (ProcessCheckException)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = guid}]});
            }
        }

        /// <summary>
        /// Gets an Eligibility check using the supplied GUID 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityItemResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
        [Consumes("application/json", "application/vnd.api+json;version=1.0")]
        [HttpGet("{guid}")]
        [HttpGet("/check/{guid}")]
        public async Task<ActionResult> EligibilityCheck(string guid)
        {
            var result = await _getEligibilityCheckItemUseCase.Execute(guid);

            if (result.IsNotFound)
            {
                return NotFound(new ErrorResponse { Errors = [new Error() {Title = guid}]});
            }

            if (!result.IsValid)
            {
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = result.ValidationErrors}]});
            }

            return new ObjectResult(result.Response) { StatusCode = StatusCodes.Status200OK };
        }
    }
}