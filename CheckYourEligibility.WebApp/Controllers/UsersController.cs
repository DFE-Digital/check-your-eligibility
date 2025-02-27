using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using CheckYourEligibility.WebApp.UseCases;
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
        private readonly ICreateOrUpdateUserUseCase _createOrUpdateUserUseCase;

        public UsersController(ILogger<UsersController> logger, ICreateOrUpdateUserUseCase createOrUpdateUserUseCase, IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _createOrUpdateUserUseCase = Guard.Against.Null(createOrUpdateUserUseCase);
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

            var response = await _createOrUpdateUserUseCase.Execute(model);
            return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };

        }
    }
}
