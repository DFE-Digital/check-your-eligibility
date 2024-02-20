﻿using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FreeSchoolMealsController : Controller
    {
        private readonly ILogger<FreeSchoolMealsController> _logger;
        private readonly IFsmCheckEligibility _service;

        public FreeSchoolMealsController(ILogger<FreeSchoolMealsController> logger, IFsmCheckEligibility service)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(service);
        }

        [ProducesResponseType(typeof(int), (int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPost]
        public async Task<ActionResult> CheckEligibility([FromBody] CheckEligibilityRequest model)
        {
            if (model == null || model.Data == null) {
                return BadRequest(new CheckEligibilityResponse() { Data = "Invalid CheckEligibilityRequest, data is required."});
                }
            model.Data.NationalInsuranceNumber = model.Data.NationalInsuranceNumber.ToUpper();
            model.Data.NationalAsylumSeekerServiceNumber = model.Data.NationalAsylumSeekerServiceNumber.ToUpper();

            var validator = new CheckEligibilityRequestDataValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                return BadRequest(new CheckEligibilityResponse(){Data = validationResults.ToString()});
            }

            var id = await _service.PostCheck(model.Data);
            var status = FsmCheckEligibilityStatus.queuedForProcessing.ToString();
            return new ObjectResult(new CheckEligibilityResponse() { Data = $"status : {status}", Links = $"eligibilityCheck: /freeSchoolMeals/{id}" }) { StatusCode = StatusCodes.Status202Accepted };
        }

        [ProducesResponseType(typeof(CheckEligibilityStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [HttpGet("{guid}/status")]
        public async Task<ActionResult> CheckEligibilityStatus(string guid)
        {
            var response = await _service.GetStatus(guid);
            if (response == null)
            {
                return NotFound(guid);
            }

            return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
