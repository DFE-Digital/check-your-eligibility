using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValuesController : ControllerBase
    {
        [HttpGet(Name = "GetValues")]
        public async Task Get()
        {
        }
    }
}
