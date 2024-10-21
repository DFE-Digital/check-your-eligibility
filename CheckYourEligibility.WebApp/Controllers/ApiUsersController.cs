using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ApiUsersController : BaseController
    {
        private readonly ILogger<ApiUsersController> _logger;
        private readonly IApiUsers _service;

        public ApiUsersController(ILogger<ApiUsersController> logger, IApiUsers service, IAudit audit)
            : base(audit)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        /// <summary>
        /// creates user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(ApiUserSaveItemResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost()]
        public async Task<ActionResult> User([FromBody] SystemUserCreateRequest model)
        {
            if (model == null || model.Data == null)
            {
                return BadRequest(new MessageResponse { Data = "Invalid request, data is required." });
            }
            if (model.Data.UserName == string.Empty || model.Data.Password == string.Empty || !model.Data.Roles.Any())
            {
                return BadRequest(new MessageResponse { Data = "Invalid request, user data is required." });
            }

            try
            {
                await _service.Create(model.Data);
                await AuditAdd(Domain.Enums.AuditType.ApiUser, model.Data.UserName);
                return new ObjectResult(new ApiUserSaveItemResponse
                {

                    Links = new ApiUserResponseLinks()
                    {
                        get_ApiUser = $"{Domain.Constants.ApiUserLinks.Link}{model.Data.UserName}",
                        put_ApiUser = $"{Domain.Constants.ApiUserLinks.Link}{JsonConvert.SerializeObject(model.Data)}",
                        delete_ApiUser = $"{Domain.Constants.ApiUserLinks.Link}{model.Data.UserName}"
                    }
                })
                { StatusCode = StatusCodes.Status201Created };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Delete User
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpDelete("{userName}")]
        public async Task<ActionResult> DeleteUser(string userName)
        {
            
            try
            {
                await _service.Delete(userName);
                await AuditAdd(Domain.Enums.AuditType.ApiUser,$"delete {userName}");

                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
        }
    }
}
