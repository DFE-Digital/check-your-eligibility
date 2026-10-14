using System.Globalization;
using System.Text.RegularExpressions;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.CsvImport;
using CheckYourEligibility.API.Gateways.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace CheckYourEligibility.API.UseCases;

public interface IImportFsmHomeOfficeDataUseCase
{
    Task Execute(IFormFile file);
}

public class ImportFsmHomeOfficeDataUseCase : IImportFsmHomeOfficeDataUseCase
{
    private static readonly Regex AsylumSeekerReferenceNumberRegex =
        new(@"^(?:\d{9}|\d{2}/\d{2}/\d{5})$", RegexOptions.Compiled);

    private readonly IAudit _auditGateway;
    private readonly IAdministration _gateway;
    private readonly ILogger<ImportFsmHomeOfficeDataUseCase> _logger;

    public ImportFsmHomeOfficeDataUseCase(IAdministration Gateway, IAudit auditGateway,
        ILogger<ImportFsmHomeOfficeDataUseCase> logger)
    {
        _gateway = Gateway;
        _auditGateway = auditGateway;
        _logger = logger;
    }

    public async Task Execute(IFormFile file)
    {
        List<FreeSchoolMealsHO> DataLoad;
        if (file == null || file.ContentType.ToLower() != "text/csv")
            throw new InvalidDataException($"{Admin.CsvfileRequired}");
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                PrepareHeaderForMatch = args => args.Header.Trim()
            };
            using (var fileStream = file.OpenReadStream())
            using (var csv = new CsvReader(new StreamReader(fileStream), config))
            {
                csv.Context.RegisterClassMap<HomeOfficeRowMap>();
                DataLoad = [];
                while (csv.Read())
                {
                    var record = csv.GetRecord<HomeOfficeRow>();
                    DataLoad.Add(new FreeSchoolMealsHO
                    {
                        FreeSchoolMealsHOID = Guid.NewGuid().ToString(),
                        FirstName = record.FirstName?.Trim(),
                        LastName = record.Surname?.Trim(),
                        DateOfBirth = DateTime.ParseExact(record.Dob?.Trim(), "yyyyMMdd",
                            CultureInfo.InvariantCulture),
                        NASS = NormalizeAsylumSeekerReferenceNumber(record.AsylumSeekerReferenceNumber),
                        PortReferenceNumber = record.PortReferenceNumber?.Trim()
                    });
                }

                if (DataLoad.Count == 0) throw new InvalidDataException("Invalid file content.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("ImportHomeOfficeData", ex);
            throw new InvalidDataException(
                $"{file.FileName} - {JsonConvert.SerializeObject(new HomeOfficeRow())} :- {ex.Message}, {ex.InnerException?.Message}");
        }

        await _gateway.ImportHomeOfficeData(DataLoad);
    }

    private static string NormalizeAsylumSeekerReferenceNumber(string value)
    {
        var trimmedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedValue) ||
            !AsylumSeekerReferenceNumberRegex.IsMatch(trimmedValue))
        {
            throw new InvalidDataException(
                "Asylum seeker reference number must contain nine digits, optionally formatted as 00/00/00000.");
        }

        return trimmedValue.Replace("/", string.Empty);
    }
}