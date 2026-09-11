using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace CheckYourEligibility.API.UseCases;

public interface IImportWfHMRCDataUseCase
{
    Task Execute(IFormFile file);
}

public class ImportWfHMRCDataUseCase : IImportWfHMRCDataUseCase
{
    private readonly IAudit _auditGateway;
    private readonly IAdministration _gateway;
    private readonly IWorkingFamiliesEvent _workingFamiliesEventGateway;
    private readonly ILogger<ImportWfHMRCDataUseCase> _logger;

    public ImportWfHMRCDataUseCase(IAdministration Gateway, IAudit auditGateway,IWorkingFamiliesEvent workingFamiliesEventGateway,
        ILogger<ImportWfHMRCDataUseCase> logger)
    {
        _gateway = Gateway;
        _workingFamiliesEventGateway = workingFamiliesEventGateway;
        _auditGateway = auditGateway;
        _logger = logger;
    }

    public async Task Execute(IFormFile file)
    {
        List<WorkingFamiliesEvent> DataLoad = new();
        if (file == null || (file.ContentType.ToLower() != "text/xml" && !file.FileName.EndsWith(".xlsm")))
            throw new InvalidDataException($"{Admin.XlsmfileRequired}");

        var validator = new WorkingFamiliesEventImportValidator();
        try
        {
            using var fileStream = file.OpenReadStream();
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(fileStream, false);
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
            WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
            var cellStyles = workbookPart.WorkbookStylesPart.Stylesheet.CellFormats.Elements<CellFormat>().ToArray();
            var sharedStrings = workbookPart.GetPartsOfType<SharedStringTablePart>().First().SharedStringTable;

            var headerRow = sheetData.Elements<Row>().ElementAt(0);
            var columnHeaders = CsvGetHelper.GetColumnHeaders(headerRow, sharedStrings);
            var eventRows = from row in headerRow.ElementsAfter()
                            where row.Elements<Cell>().ElementAt(1).CellValue is not null
                            select row;
            foreach (Row row in eventRows)
            {
                List<string> eventProps = [];
                foreach (Cell cell in row.Elements<Cell>().Skip(1))
                {
                    try
                    {
                        CellFormat style = cellStyles[int.Parse(cell.StyleIndex.InnerText)];
                        var cellValueString = CsvGetHelper.getCellValueString(cell, sharedStrings, cellStyles);
                        eventProps.Add(cellValueString);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"Failed to parse data at {cell.CellReference}:- {ex.Message}");
                    }
                }

                var wfEvent = WorkingFamiliesEventHelper.ParseWorkingFamiliesEvent(eventProps, columnHeaders);
                var validationResults = validator.Validate(wfEvent);
                if (!validationResults.IsValid) throw new ValidationException($"On row {row.RowIndex}: {validationResults.ToString().ReplaceLineEndings(", ")}");
                DataLoad.Add(wfEvent);
            }
            if (DataLoad == null || DataLoad.Count == 0) throw new InvalidDataException("Invalid file no content.");
        }
        catch (Exception ex)
        {
            _logger.LogError("ImportWfHMRCData", ex);
            throw new InvalidDataException(
                $"{file.FileName} - {JsonConvert.SerializeObject(new WorkingFamiliesEvent())} :- {ex.Message}, {ex.InnerException?.Message}");
        }

             

        try
        {

            // for each code check if it is a new event
            // if old events found
            // check if the new event is reconfirmed on time (contigous)
            // true - only update the VED and GPED from the new event
            // false - update VSD , VED and GPED from the new event
            // else if no historical event found for this code
            // create a new summary event using the data from the new event
            for (int i = 0; i < DataLoad.Count; i++)
            {
                // parse PI to the summary record 
                var eventSummaryRecord = WorkingFamiliesEventHelper.ParsePIWorkingFamilySummaryFromWorkingFamilyEvent(DataLoad[i]);
                //check for exisitng records
                var eventRecords = await _workingFamiliesEventGateway.GetWorkingFamiliesEventsByEligibilityCode(DataLoad[i].EligibilityCode);
                //if older events found, initiate contigous logic
                if (eventRecords.Any())
                {                   
                    //Check if event is contigous and set VSD to earliest VSD of the current contiguous block
                    // if newEvent.VSD <= olderEvent.VED (reconfirmed before the end of the reconfirmaion window)
                    // and newEvent.VSD <= olderEvent.GPED (reconfirmation is before the )
                    for (int e = 0; e < eventRecords.Count; e++)
                    {
                        if (DataLoad[i].ValidityStartDate <= eventRecords[e].GracePeriodEndDate)
                        {
                            //  DataLoad[i].DiscretionaryValidityStartDate = eventRecords[e].DiscretionaryValidityStartDate;
                            //  DataLoad[i].ValidityStartDate = eventRecords[e].ValidityStartDate;
                        }
                        else
                        {
                            break;
                        }
                                              
                    eventSummaryRecord.ValidityEndDate = eventRecords[0].ValidityEndDate;
                    eventSummaryRecord.GracePeriodEndDate = eventRecords[0].GracePeriodEndDate;
                    
                    }
                }

                await _gateway.ImportWfHMRCData(DataLoad);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError("ImportWfHMRCData", ex);
            throw;
        }


    }

}