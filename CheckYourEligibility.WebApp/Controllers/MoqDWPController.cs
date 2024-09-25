using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Domain.Responses.DWP;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Xml.Linq;

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

        [ProducesResponseType(typeof(DwpMatchResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpPost]
        public async Task<ActionResult> Match(
            [FromBody] CitizenMatchRequest model,
            [FromHeader(Name = "instigating-user-id")][Required] string investigatingUserId = "abcdef1234577890abcdeffghi",
            [FromHeader(Name = "policy-id")][Required] string policy_id = "ece",
            [FromHeader(Name = "correlation-id")][Required] string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
            [FromHeader(Name = "context")][Required] string context = "abc-1-ab-x12888"
            )
        {


            if (model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameEligible.ToUpper()
                || model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameNotEligible.ToUpper())
            {
                return new ObjectResult(new DwpMatchResponse()
                {
                    Data = new DwpMatchResponse.DwpResponse_Data
                    {
                        Id = model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameEligible.ToUpper() ? MogDWPValues.validCitizenEligibleGuid : MogDWPValues.validCitizenNotEligibleGuid,
                        Type = "MatchResult",
                        Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                    }
                    ,
                    Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
                })
                { StatusCode = StatusCodes.Status200OK };
            }
            else if (model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameDuplicatesFound.ToUpper())
            {
                return UnprocessableEntity();
            }
            else
            {
                return NotFound();
            }

        }


        //Benefit type is not currently being used as I cant figure out how to pass in an array on a get Request, so all claims are returned
        [ProducesResponseType(typeof(DwpClaimsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpGet("{Guid}/Claims")]
        public async Task<ActionResult> Claim(
           string Guid, string BenefitType = "", string EffectiveFromDate = "", string EffectiveToDate = "",
           [FromHeader(Name = "instigating-user-id")][Required] string investigatingUserId = "abcdef1234577890abcdeffghi",
           [FromHeader(Name = "access-level")][Required] int accessLevel = 1,
           [FromHeader(Name = "correlation-id")][Required] string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
           [FromHeader(Name = "context")][Required] string context = "abc-1-ab-x12888")
        {
            if (Guid == MogDWPValues.validCitizenEligibleGuid)
            {
                var response = JsonConvert.DeserializeObject<DwpClaimsResponse>(GetClaimResponse(BenefitType));

                return new ObjectResult(response)
                { StatusCode = StatusCodes.Status200OK };
            }
            else if (Guid == MogDWPValues.validCitizenNotEligibleGuid)
            {
                return new NotFoundResult();
            }
            else
            {
                return BadRequest();
            }
        }

        private string GetClaimResponse(string benefitType)
        {
            Enum.TryParse(benefitType, out DwpBenefitType dwpBenefitType);

            switch (dwpBenefitType)
            {
                case DwpBenefitType.employment_support_allowance_income_based:
                // return Properties.Resources.;
                case DwpBenefitType.job_seekers_allowance_income_based:
                // return Properties.Resources.;
                case DwpBenefitType.pensions_credit:
                    return Properties.Resources.DwpClaims_pensions_credit;
                case DwpBenefitType.income_support:
                    return Properties.Resources.DwpClaims_income_support;
                case DwpBenefitType.universal_credit:
                    return Properties.Resources.DwpClaims_universal_credit;
                default:
                    return Properties.Resources.DwpClaims_all;
            }
        }
    }
}
