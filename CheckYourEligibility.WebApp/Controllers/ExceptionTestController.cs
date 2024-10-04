using Azure;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
namespace CheckYourEligibility.WebApp.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class ExceptionTestController : ControllerBase
    {
        // GET: api/ExceptionTest/Trigger
        [HttpGet("Trigger")]
        public IActionResult TriggerException()
        {
            // Intentionally throw an exception to test logging
            throw new InvalidOperationException("This is a test exception for Azure Application Insights.");
        }

        [HttpPost("Trigger")]
        public IActionResult TriggerPostException([FromBody] CheckEligibilityRequest model)
        {
            // Intentionally throw an exception to test logging
            throw new InvalidOperationException("This is a test Post exception for Azure Application Insights.");
        }

        [HttpPost("TriggerBadRequest")]
        public IActionResult TriggerPostBadRequestResponse([FromBody] CheckEligibilityRequest model)
        {
            return BadRequest(new MessageResponse { Data = "Rest response data" });
        }

        [HttpPost("TriggerOkRequest")]
        public IActionResult TriggerPostOkResponse([FromBody] CheckEligibilityRequest model)
        {
           
            return new ObjectResult(new CheckEligibilityResponse()
            {
                Data = new StatusValue() { Status = "this is a good status" },
                
            })
            { StatusCode = StatusCodes.Status202Accepted };

        }
    }


}
