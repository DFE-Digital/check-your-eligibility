using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses.DWP;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{

    [ApiController]
    [Route("[controller]/v2/citizens/")]
    public class MoqDWPController : Controller
    {
        private readonly ILogger<FreeSchoolMealsController> _logger;
        private readonly IFsmCheckEligibility _service;

        public MoqDWPController(ILogger<FreeSchoolMealsController> logger, IFsmCheckEligibility service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        [ProducesResponseType(typeof(DwpResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPost]
        //public async Task<ActionResult> Match(
        //    [FromBody] CitizenMatchRequest model
        //    )
        public async Task<ActionResult> Match(
            [FromBody] CitizenMatchRequest model,
            [FromHeader(Name = "instigating-user-id")][Required] string investigatingUserId = "abcdef1234577890abcdeffghi",
            [FromHeader(Name = "policy-id")][Required] string policy_id = "fsm",
            [FromHeader(Name = "correlation-id")][Required] string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
            [FromHeader(Name = "context")][Required] string context = "abc-1-ab-x12888"
            )
        {


            if (model?.Data?.Attributes?.LastName == MogDWPValues.validCitizenSurnameEligible || model?.Data?.Attributes?.LastName == MogDWPValues.validCitizenSurnameNotEligible)
            {
                return new ObjectResult(new DwpResponse()
                {
                    Data = new DwpResponse.DwpResponse_Data
                    {
                        Id = model?.Data?.Attributes?.LastName == MogDWPValues.validCitizenSurnameEligible ? MogDWPValues.validCitizenEligibleGuid : MogDWPValues.validCitizenNotEligibleGuid,
                        Type = "MatchResult",
                        Attributes = new DwpResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                    }
                    ,
                    Jsonapi = new DwpResponse.DwpResponse_Jsonapi { Version = "2.0" }
                })
                { StatusCode = StatusCodes.Status200OK };
            }
            else
            {
                return NotFound();
            }

        }

        [ProducesResponseType(typeof(DwpResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpGet("{Guid}/Claims")]
        public async Task<ActionResult> Claim(
           string Guid, string BenefitType, string EffectiveFromDate = "", string EffectiveToDate = "",
           [FromHeader(Name = "instigating-user-id")][Required] string investigatingUserId = "abcdef1234577890abcdeffghi",
           [FromHeader(Name = "access-level")][Required] int accessLevel = 1,
           [FromHeader(Name = "correlation-id")][Required] string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
           [FromHeader(Name = "context")][Required] string context = "abc-1-ab-x12888"
           )
        {
            if (Guid == MogDWPValues.validCitizenEligibleGuid
                    && BenefitType == MogDWPValues.validUniversalBenefitType
                    )
            {
                return new OkResult();
            }
            else if(Guid == MogDWPValues.validCitizenNotEligibleGuid)
            {
                return new NotFoundResult();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
