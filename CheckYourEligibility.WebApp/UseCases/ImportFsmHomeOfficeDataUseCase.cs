using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface IImportFsmHomeOfficeDataUseCase
    {
        Task Execute(IFormFile file);
    }

    public class ImportFsmHomeOfficeDataUseCase : IImportFsmHomeOfficeDataUseCase
    {
        private readonly IAdministration _service;
        private readonly IAudit _auditService;
        private readonly ILogger<ImportFsmHomeOfficeDataUseCase> _logger;

        public ImportFsmHomeOfficeDataUseCase(IAdministration service, IAudit auditService, ILogger<ImportFsmHomeOfficeDataUseCase> logger)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task Execute(IFormFile file)
        {
            List<FreeSchoolMealsHO> DataLoad;
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                throw new InvalidDataException($"{Admin.CsvfileRequired}");
            }
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    BadDataFound = null,
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
                throw new InvalidDataException($"{file.FileName} - {JsonConvert.SerializeObject(new HomeOfficeRow())} :- {ex.Message}, {ex.InnerException?.Message}");
            }

            await _service.ImportHomeOfficeData(DataLoad);
            await _auditService.CreateAuditEntry(AuditType.Administration, string.Empty);
        }
    }
}