// Ignore Spelling: Fsm

using Ardalis.GuardClauses;
using Azure;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AdministrationController : BaseController
    {
        private readonly ILogger<AdministrationController> _logger;
        private readonly IAdministration _service;

        public AdministrationController(ILogger<AdministrationController> logger, IAdministration service, IAudit audit)
            : base(audit)
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
            await AuditAdd(Domain.Enums.AuditType.Administration, string.Empty);
            return new ObjectResult(new MessageResponse { Data = $"{Admin.EligibilityChecksCleanse}" }) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Imports School Establishments
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPost("/importEstablishments")]
        public async Task<ActionResult> ImportEstablishments(IFormFile file)
        {
            List<EstablishmentRow> DataLoad;
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                return BadRequest(new MessageResponse { Data = $"{Admin.CsvfileRequired}" });
            }
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    BadDataFound = null,
                    MissingFieldFound = null
                };
                using (var fileStream = file.OpenReadStream())

                using (var csv = new CsvReader(new StreamReader(fileStream), config))
                {
                    csv.Context.RegisterClassMap<EstablishmentRowMap>();
                    DataLoad = csv.GetRecords<EstablishmentRow>().ToList();

                    if (DataLoad == null || !DataLoad.Any())
                    {
                        throw new InvalidDataException("Invalid file content.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportEstablishmentData", ex);
                return new ObjectResult(new MessageResponse
                {
                    Data = $"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message}," +
                    $"{ex.InnerException?.Message}"
                })
                { StatusCode = StatusCodes.Status400BadRequest };
            }

            await _service.ImportEstablishments(DataLoad);
            await AuditAdd(Domain.Enums.AuditType.Administration, string.Empty);
            return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.EstablishmentFileProcessed}"}){ StatusCode = StatusCodes.Status200OK };
        }


        /// <summary>
        /// Truncates FsmHomeOfficeData and imports a new data set
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        [HttpPost("/importFsmHomeOfficeData")]
        public async Task<ActionResult> ImportFsmHomeOfficeData(IFormFile file)
        {
            List<FreeSchoolMealsHO> DataLoad;
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                return BadRequest(new MessageResponse { Data = $"{Admin.CsvfileRequired}" });
            }
            try
            {
               
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    BadDataFound = null, //arg => badRows.Add(arg.Context.Parser.RawRecord),
                    MissingFieldFound = null
                };
                using (var fileStream = file.OpenReadStream())

                using (var csv = new CsvReader(new StreamReader(fileStream), config))
                {
                    csv.Context.RegisterClassMap<HomeOfficeRowMap>();
                    var records = csv.GetRecords<HomeOfficeRow>();
                    
                    DataLoad = records.Select(x => new FreeSchoolMealsHO
                    {
                        FreeSchoolMealsHOID = Guid.NewGuid().ToString(),
                        NASS = x.Nas,
                        LastName = x.Surname,
                        DateOfBirth = DateTime.ParseExact(x.Dob, "yyyyMMdd", CultureInfo.InvariantCulture)
                    }).ToList();
                    if (DataLoad == null || DataLoad.Count == 0)
                    {
                        throw new InvalidDataException("Invalid file content.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportHomeOfficeData", ex);
                return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {JsonConvert.SerializeObject(new HomeOfficeRow())} :- {ex.Message}," +
                    $"{ex.InnerException?.Message}" }) { StatusCode = StatusCodes.Status400BadRequest };
            }

            await _service.ImportHomeOfficeData(DataLoad);
            await AuditAdd(Domain.Enums.AuditType.Administration, string.Empty);
            return new ObjectResult(new MessageResponse { Data = $"{file.FileName} - {Admin.HomeOfficeFileProcessed}" }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
