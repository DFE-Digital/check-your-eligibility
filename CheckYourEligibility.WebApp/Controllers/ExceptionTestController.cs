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
    }


}
