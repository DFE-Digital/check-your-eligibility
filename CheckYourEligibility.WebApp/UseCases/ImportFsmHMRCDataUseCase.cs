using Ardalis.GuardClauses;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Services.Interfaces;
using Newtonsoft.Json;
using System.Globalization;
using System.Xml.Linq;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface IImportFsmHMRCDataUseCase
    {
        Task Execute(IFormFile file);
    }

    public class ImportFsmHMRCDataUseCase : IImportFsmHMRCDataUseCase
    {
        private readonly IAdministration _service;
        private readonly IAudit _auditService;
        private readonly ILogger<ImportFsmHMRCDataUseCase> _logger;

        public ImportFsmHMRCDataUseCase(IAdministration service, IAudit auditService, ILogger<ImportFsmHMRCDataUseCase> logger)
        {
            _service = Guard.Against.Null(service);
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
        }

        public async Task Execute(IFormFile file)
        {
            List<FreeSchoolMealsHMRC> DataLoad = new();
            if (file == null || file.ContentType.ToLower() != "text/xml")
            {
                throw new InvalidDataException($"{Admin.XmlfileRequired}");
            }
            try
            {
                using var fileStream = file.OpenReadStream();
                XElement po = XElement.Load(fileStream);
                IEnumerable<XElement> childElements =
                    from el in po.Elements()
                    select el;
                var EligiblePersons = childElements.FirstOrDefault(x => x.Name.LocalName == "EligiblePersons");
                if (EligiblePersons != null)
                    DataLoad.AddRange(from XElement EligiblePerson in EligiblePersons.Nodes()
                                    let elements = EligiblePerson.Elements()
                                    let item = new FreeSchoolMealsHMRC()
                                    {
                                        FreeSchoolMealsHMRCID = elements.First(x => x.Name.LocalName == "NINO").Value,
                                        DataType = Convert.ToInt32(elements.First(x => x.Name.LocalName == "DataType").Value),
                                        Surname = elements.First(x => x.Name.LocalName == "Surname").Value,
                                        DateOfBirth = DateTime.ParseExact(elements.First(x => x.Name.LocalName == "DateOfBirth").Value, "ddMMyyyy", CultureInfo.InvariantCulture)
                                    }
                                    select item);

                if (DataLoad == null || DataLoad.Count == 0)
                {
                    throw new InvalidDataException("Invalid file no content.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ImportHMRCData", ex);
                throw new InvalidDataException($"{file.FileName} - {JsonConvert.SerializeObject(new FreeSchoolMealsHMRC())} :- {ex.Message}, {ex.InnerException?.Message}");
            }

            await _service.ImportHMRCData(DataLoad);
            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.Administration, string.Empty);
            if (auditData != null)
            {
                await _auditService.AuditAdd(auditData);
            }
        }
    }
}