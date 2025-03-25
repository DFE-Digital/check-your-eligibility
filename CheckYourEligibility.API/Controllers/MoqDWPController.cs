using System.ComponentModel.DataAnnotations;
using System.Net;
using CheckYourEligibility.API.Boundary.Requests.DWP;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Boundary.Responses.DWP;
using CheckYourEligibility.API.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.API.Controllers;

[ApiController]
[Route("[controller]/v2/citizens/")]
public class MoqDWPController : Controller
{
    private readonly IGetCitizenClaimsUseCase _getCitizenClaimsUseCase;
    private readonly ILogger<MoqDWPController> _logger;
    private readonly IMatchCitizenUseCase _matchCitizenUseCase;

    public MoqDWPController(ILogger<MoqDWPController> logger, IMatchCitizenUseCase matchCitizenUseCase,
        IGetCitizenClaimsUseCase getCitizenClaimsUseCase)
    {
        _logger = logger;
        _matchCitizenUseCase = matchCitizenUseCase;
        _getCitizenClaimsUseCase = getCitizenClaimsUseCase;
    }

    [ProducesResponseType(typeof(DwpMatchResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [HttpPost]
    public async Task<ActionResult> Match(
        [FromBody] CitizenMatchRequest model,
        [FromHeader(Name = "instigating-user-id")] [Required]
        string investigatingUserId = "abcdef1234577890abcdeffghi",
        [FromHeader(Name = "policy-id")] [Required]
        string policy_id = "ece",
        [FromHeader(Name = "correlation-id")] [Required]
        string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
        [FromHeader(Name = "context")] [Required]
        string context = "abc-1-ab-x12888"
    )
    {
        try
        {
            var response = await _matchCitizenUseCase.Execute(model);
            if (response != null) return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };

            return NotFound();
        }
        catch (InvalidOperationException)
        {
            return UnprocessableEntity();
        }
    }

    [ProducesResponseType(typeof(DwpClaimsResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [HttpGet("{Guid}/Claims")]
    public async Task<ActionResult> Claim(
        string Guid, string BenefitType = "", string EffectiveFromDate = "", string EffectiveToDate = "",
        [FromHeader(Name = "instigating-user-id")] [Required]
        string investigatingUserId = "abcdef1234577890abcdeffghi",
        [FromHeader(Name = "access-level")] [Required]
        int accessLevel = 1,
        [FromHeader(Name = "correlation-id")] [Required]
        string correlationId = "4c6a63f1-1924-4911-b45c-95dbad8b6c37",
        [FromHeader(Name = "context")] [Required]
        string context = "abc-1-ab-x12888")
    {
        try
        {
            var response = await _getCitizenClaimsUseCase.Execute(Guid, BenefitType);
            if (response != null) return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };

            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}