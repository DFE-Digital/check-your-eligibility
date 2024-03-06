﻿using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdministrationController : Controller
    {
        private readonly ILogger<AdministrationController> _logger;
        private readonly IAdministration _service;

        public AdministrationController(ILogger<AdministrationController> logger, IAdministration service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        /// <summary>
        /// Deletes all old Eligibility Checks based on the service configuration
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPut("/cleanUpEligibilityChecks")]
        public async Task<ActionResult> CleanUpEligibilityChecks()
        {
            await _service.CleanUpEligibilityChecks();
            return new ObjectResult(new BaseResponse() { Data = $"EligibilityChecks deleted based on configuration settings.", Links = string.Empty }) { StatusCode = StatusCodes.Status200OK };
        }

    }
}