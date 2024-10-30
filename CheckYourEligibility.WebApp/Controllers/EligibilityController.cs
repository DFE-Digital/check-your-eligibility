using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Migrations.Migrations;
using CheckYourEligibility.Domain.Exceptions;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using CheckEligibilityStatusResponse = CheckYourEligibility.Domain.Responses.CheckEligibilityStatusResponse;
using StatusValue = CheckYourEligibility.Domain.Responses.StatusValue;

namespace CheckYourEligibility.WebApp.Controllers
{
    //EligibilityController

    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class EligibilityCheckController : BaseController
    {
        private readonly ICheckEligibility _checkService;
        
        private readonly ILogger<EligibilityCheckController> _logger;
        private readonly int _bulkUploadRecordCountLimit;


        public EligibilityCheckController(ILogger<EligibilityCheckController> logger, ICheckEligibility checkService, IAudit audit, IConfiguration configuration)
            : base( audit)
        {
            _logger = Guard.Against.Null(logger);
            _checkService = Guard.Against.Null(checkService);
            _bulkUploadRecordCountLimit = configuration.GetValue<int>("BulkEligibilityCheckLimit");
        }

        /// <summary>
        /// Posts a FSM Eligibility Check to the processing queue
        /// </summary>
        /// <param name="CheckEligibilityRequest"></param>
        /// <remarks>If the check has already been submitted, then the stored Hash is returned</remarks>
        /// <links cref="https://stackoverflow.com/questions/61896978/asp-net-core-swaggerresponseexample-not-outputting-specified-example"/>
        [ProducesResponseType(typeof(CheckEligibilityResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost("FreeSchoolMeals")]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest_Fsm model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid Request, data is required." });
            }

            return await PostCheck(model);
        }

        /// <summary>
        /// Posts the array of FSM checks
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(CheckEligibilityResponse), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost("/EligibilityCheck/FreeSchoolMeals/Bulk")]
        public async Task<ActionResult> CheckEligibilityBulk([FromBody] CheckEligibilityRequestBulk_Fsm model)
        {
            try
            {
                if (model == null || model.Data == null)
                {
                    return BadRequest(new MessageResponse { Data = "Invalid Request, data is required." });
                }
                if (model.Data.Count()> _bulkUploadRecordCountLimit)
                {
                    return BadRequest(new MessageResponse { Data = $"Invalid Request, data limit of {_bulkUploadRecordCountLimit} exceeded, {model.Data.Count()} records."});
                }

                return await ProcessBulk(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
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
            try
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
                    { Get_BulkCheck_Results = $"{Domain.Constants.CheckLinks.BulkCheckLink}{guid}{Domain.Constants.CheckLinks.BulkCheckResults}" }
                })
                { StatusCode = StatusCodes.Status200OK };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
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
            try
            {
                var response = await _checkService.GetBulkCheckResults<IList<CheckEligibilityItem>>(guid);
                if (response == null)
                {
                    return NotFound(guid);
                }
                await AuditAdd(Domain.Enums.AuditType.CheckBulkResults, guid);
                return new ObjectResult(new CheckEligibilityBulkResponse()
                {
                    Data = response as List<CheckEligibilityItem>,
                })
                { StatusCode = StatusCodes.Status200OK };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
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
            try
            {
                var response = await _checkService.GetStatus(guid);
                if (response == null)
                {
                    return NotFound(guid);
                }
                await AuditAdd(Domain.Enums.AuditType.Check, guid);

                return new ObjectResult(new CheckEligibilityStatusResponse() { Data = new StatusValue() { Status = response.Value.ToString() } }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
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
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
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
            try
            {

                var response = await _checkService.GetItem<CheckEligibilityItem>(guid);
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
                        Get_EligibilityCheck = $"{Domain.Constants.CheckLinks.GetLink}{guid}",
                        Put_EligibilityCheckProcess = $"{Domain.Constants.CheckLinks.ProcessLink}{guid}",
                        Get_EligibilityCheckStatus = $"{Domain.Constants.CheckLinks.GetLink}{guid}/Status"
                    }
                })
                { StatusCode = StatusCodes.Status200OK };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
        }

        public async Task<ActionResult> PostCheck<T>(T model)
        {
            try
            {
                switch (model)
                {
                    case CheckEligibilityRequest_Fsm requestData:
                        {
                            var validationResults = Validate_Fsm(requestData);
                            if (!validationResults.IsValid)
                                return BadRequest(new MessageResponse { Data = validationResults.ToString() });

                            var response = await _checkService.PostCheck(requestData.Data);
                            return await GetPostResponse(response);
                        }
                    default:
                        throw new Exception($"Unknown request type:-{model.GetType()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
        }

        private async Task<ActionResult> GetPostResponse(PostCheckResult response)
        {
            await AuditAdd(Domain.Enums.AuditType.Check, response.Id);

            return new ObjectResult(new CheckEligibilityResponse()
            {
                Data = new StatusValue() { Status = response.Status.ToString() },
                Links = new CheckEligibilityResponseLinks
                {
                    Get_EligibilityCheck = $"{Domain.Constants.CheckLinks.GetLink}{response.Id}",
                    Put_EligibilityCheckProcess = $"{Domain.Constants.CheckLinks.ProcessLink}{response.Id}",
                    Get_EligibilityCheckStatus = $"{Domain.Constants.CheckLinks.GetLink}{response.Id}/Status"
                }
            })
            { StatusCode = StatusCodes.Status202Accepted };
        }

        private static ValidationResult Validate_Fsm(CheckEligibilityRequest_Fsm model)
        {
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber?.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator_Fsm();
            var validationResults = validator.Validate(model.Data);
            return validationResults;
        }

        private async Task<ActionResult> ProcessBulk<T>(T model) where T : CheckEligibilityRequestBulk_Fsm
        {
            StringBuilder validationResultsItems = ValidateBulkItems(model);

            if (validationResultsItems.Length > 0)
            {
                return BadRequest(new MessageResponse { Data = validationResultsItems.ToString() });
            }

            var groupId = Guid.NewGuid().ToString();
            switch (model)
            {
                case CheckEligibilityRequestBulk_Fsm request:
                    await _checkService.PostCheck(request.Data, groupId);
                    break;
                default:
                    break;
            }

            await AuditAdd(Domain.Enums.AuditType.BulkCheck, groupId);

            return new ObjectResult(new CheckEligibilityResponseBulk()
            {
                Data = new StatusValue() { Status = $"{Domain.Constants.Messages.Processing}" },
                Links = new CheckEligibilityResponseBulkLinks
                {
                    Get_Progress_Check = $"{Domain.Constants.CheckLinks.BulkCheckLink}{groupId}{Domain.Constants.CheckLinks.BulkCheckProgress}",
                    Get_BulkCheck_Results = $"{Domain.Constants.CheckLinks.BulkCheckLink}{groupId}{Domain.Constants.CheckLinks.BulkCheckResults}"
                }
            })
            { StatusCode = StatusCodes.Status202Accepted };
        }

        private static StringBuilder ValidateBulkItems<T>(T model)
        {
            var validationResultsItems = new StringBuilder();
            switch (model)
            {
                case CheckEligibilityRequestBulk_Fsm requestData:
                    {

                        var validator = new CheckEligibilityRequestDataValidator_Fsm();
                        var sequence = 1;

                        foreach (var item in requestData.Data)
                        {
                            item.NationalInsuranceNumber = item.NationalInsuranceNumber?.ToUpper();
                            item.NationalAsylumSeekerServiceNumber = item.NationalAsylumSeekerServiceNumber?.ToUpper();
                            item.Sequence = sequence;
                            var validationResults = validator.Validate(item);
                            if (!validationResults.IsValid)
                            {
                                validationResultsItems.AppendLine($"Item:-{sequence}, {validationResults.ToString()}");
                            }
                            sequence++;
                        }
                        break;
                    }
            }
            return validationResultsItems;
        }

    }
 }
