using CheckYourEligibility.Domain.Requests;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FreeSchoolMealsController : Controller
    {
       
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest model)
        {
            model.Data.NiNumber = model.Data.NiNumber.ToUpper();
            model.Data.NASSNumber = model.Data.NASSNumber.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(new CheckEligibilityResponse(){Data = validationResults.ToString()});
            }

            //var id = await _service.PostAccessRequest(model);
            var id = "123";

            return new ObjectResult(new { Id = id }) { StatusCode = StatusCodes.Status202Accepted };
        }
    }
}
