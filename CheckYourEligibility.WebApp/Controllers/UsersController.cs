using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUsers _service;

        public UsersController(ILogger<UsersController> logger, IUsers service, IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        /// <summary>
        /// creates or returns existing user Id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(UserSaveItemResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost()]
        public async Task<ActionResult> User([FromBody] UserCreateRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid request, data is required." });
            }

            var response = await _service.Create(model.Data);
            await AuditAdd(Domain.Enums.AuditType.User, response);
            return new ObjectResult(new UserSaveItemResponse
            {
                Data = response
            })
            { StatusCode = StatusCodes.Status201Created };

        }
    }
}
