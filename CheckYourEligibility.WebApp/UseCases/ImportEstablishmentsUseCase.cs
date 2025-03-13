using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Services.CsvImport;
using CheckYourEligibility.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System.Globalization;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface IImportEstablishmentsUseCase
    {
        Task Execute(IFormFile file);
    }

    public class ImportEstablishmentsUseCase : IImportEstablishmentsUseCase
    {
        private readonly IAdministration _service;
        private readonly IAudit _auditService;
        private readonly ILogger<ImportEstablishmentsUseCase> _logger;

        public ImportEstablishmentsUseCase(IAdministration service, IAudit auditService, ILogger<ImportEstablishmentsUseCase> logger)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task Execute(IFormFile file)
        {
            List<EstablishmentRow> DataLoad;
            if (file == null || file.ContentType.ToLower() != "text/csv")
            {
                throw new InvalidDataException($"{Admin.CsvfileRequired}");
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

                    if (DataLoad == null || DataLoad.Count == 0)
                    {
                        throw new InvalidDataException("Invalid file content.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportEstablishmentData", ex);
                throw new InvalidDataException($"{file.FileName} - {JsonConvert.SerializeObject(new EstablishmentRow())} :- {ex.Message}, {ex.InnerException?.Message}");
            }

            await _service.ImportEstablishments(DataLoad);
            await _auditService.CreateAuditEntry(AuditType.Administration, string.Empty);
        }
    }
}