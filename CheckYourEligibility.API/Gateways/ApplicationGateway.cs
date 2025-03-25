﻿// Ignore Spelling: Fsm

using System.Globalization;
using AutoMapper;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Gateways.Interfaces;
using Microsoft.EntityFrameworkCore;
using ApplicationStatus = CheckYourEligibility.API.Domain.Enums.ApplicationStatus;

namespace CheckYourEligibility.API.Gateways;

public class ApplicationGateway : BaseGateway, IApplication
{
    private const int referenceMaxValue = 99999999;
    private static Random randomNumber;
    private readonly IEligibilityCheckContext _db;
    private readonly int _hashCheckDays;
    private readonly ILogger _logger;
    protected readonly IMapper _mapper;

    public ApplicationGateway(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper,
        IConfiguration configuration)
    {
        _logger = logger.CreateLogger("ServiceCheckEligibility");
        _db = dbContext;
        _mapper = mapper;

        randomNumber ??= new Random(referenceMaxValue);
        _hashCheckDays = configuration.GetValue<short>("HashCheckDays");
    }

    public async Task<ApplicationResponse> PostApplication(ApplicationRequestData data)
    {
        try
        {
            var item = _mapper.Map<Application>(data);
            var hashCheck = GetHash(data.Type, item);
            if (hashCheck == null) throw new Exception($"No Check found. Type:- {data.Type}");
            item.ApplicationID = Guid.NewGuid().ToString();
            item.Type = hashCheck.Type;
            item.Reference = GetReference();
            item.EligibilityCheckHashID = hashCheck?.EligibilityCheckHashID;
            item.Created = DateTime.UtcNow;
            item.Updated = DateTime.UtcNow;

            if (hashCheck.Outcome == CheckEligibilityStatus.eligible)
                item.Status = ApplicationStatus.Entitled;
            else
                item.Status = ApplicationStatus.EvidenceNeeded;

            try
            {
                var establishment = _db.Establishments
                    .Include(x => x.LocalAuthority)
                    .First(x => x.EstablishmentId == data.Establishment);
                item.LocalAuthorityId = establishment.LocalAuthorityId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to find school:- {data.Establishment}, {ex.Message}");
            }


            await _db.Applications.AddAsync(item);
            await AddStatusHistory(item, ApplicationStatus.Entitled);

            await _db.SaveChangesAsync();

            var saved = _db.Applications
                .First(x => x.ApplicationID == item.ApplicationID);

            TrackMetric($"Application {item.Type}", 1);

            return await GetApplication(saved.ApplicationID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Db post application");
            throw;
        }
    }


    public async Task<ApplicationResponse?> GetApplication(string guid)
    {
        var result = await _db.Applications
            .Include(x => x.Statuses)
            .Include(x => x.Establishment)
            .ThenInclude(x => x.LocalAuthority)
            .Include(x => x.User)
            .Include(x => x.EligibilityCheckHash)
            .FirstOrDefaultAsync(x => x.ApplicationID == guid);
        if (result != null)
        {
            var item = _mapper.Map<ApplicationResponse>(result);
            item.CheckOutcome = new ApplicationResponse.ApplicationHash
                { Outcome = result.EligibilityCheckHash?.Outcome.ToString() };
            return item;
        }

        return null;
    }

    public async Task<ApplicationSearchResponse> GetApplications(ApplicationRequestSearch model)
    {
        IQueryable<Application> query;

        if (model.Data.Statuses != null && model.Data.Statuses.Any())
            query = _db.Applications.Where(a => model.Data.Statuses.Contains(a.Status.Value));
        else
            query = _db.Applications;

        // Apply other filters
        query = ApplyAdditionalFilters(query, model);

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalRecords / model.PageSize);

        // Pagination
        model.PageNumber = model.PageNumber <= 0 ? 1 : model.PageNumber;
        var pagedResults = await query
            .Skip((model.PageNumber - 1) * model.PageSize)
            .Take(model.PageSize)
            .Include(x => x.Statuses)
            .Include(x => x.Establishment)
            .ThenInclude(x => x.LocalAuthority)
            .Include(x => x.User)
            .ToListAsync();


        var mappedResults = _mapper.Map<IEnumerable<ApplicationResponse>>(pagedResults);

        return new ApplicationSearchResponse
        {
            Data = mappedResults,
            TotalRecords = totalRecords,
            TotalPages = totalPages
        };
    }

    public async Task<ApplicationStatusUpdateResponse> UpdateApplicationStatus(string guid, ApplicationStatusData data)
    {
        var result = await _db.Applications.FirstOrDefaultAsync(x => x.ApplicationID == guid);
        if (result != null)
        {
            result.Status = data.Status;
            await AddStatusHistory(result, result.Status.Value);

            result.Updated = DateTime.UtcNow;
            var updates = await _db.SaveChangesAsync();
            TrackMetric($"Application Status Change {result.Status}", 1);
            TrackMetric($"Application Status Change Establishment:-{result.EstablishmentId} {result.Status}", 1);
            TrackMetric($"Application Status Change La:-{result.LocalAuthorityId} {result.Status}", 1);
            return new ApplicationStatusUpdateResponse
                { Data = new ApplicationStatusDataResponse { Status = result.Status.Value.ToString() } };
        }

        return null;
    }

    private IQueryable<Application> ApplyAdditionalFilters(IQueryable<Application> query,
        ApplicationRequestSearch model)
    {
        query = query.Where(x => x.Type == model.Data.Type);

        if (model.Data?.Establishment != null)
            query = query.Where(x => x.EstablishmentId == model.Data.Establishment);
        if (model.Data?.LocalAuthority != null)
            query = query.Where(x => x.LocalAuthorityId == model.Data.LocalAuthority);

        if (!string.IsNullOrEmpty(model.Data?.ParentNationalInsuranceNumber))
            query = query.Where(x => x.ParentNationalInsuranceNumber == model.Data.ParentNationalInsuranceNumber);
        if (!string.IsNullOrEmpty(model.Data?.ParentLastName))
            query = query.Where(x => x.ParentLastName == model.Data.ParentLastName);
        if (!string.IsNullOrEmpty(model.Data?.ParentNationalAsylumSeekerServiceNumber))
            query = query.Where(x =>
                x.ParentNationalAsylumSeekerServiceNumber == model.Data.ParentNationalAsylumSeekerServiceNumber);
        if (!string.IsNullOrEmpty(model.Data?.ParentDateOfBirth))
            query = query.Where(x =>
                x.ParentDateOfBirth == DateTime.ParseExact(model.Data.ParentDateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(model.Data?.ChildLastName))
            query = query.Where(x => x.ChildLastName == model.Data.ChildLastName);
        if (!string.IsNullOrEmpty(model.Data?.ChildDateOfBirth))
            query = query.Where(x =>
                x.ChildDateOfBirth == DateTime.ParseExact(model.Data.ChildDateOfBirth, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(model.Data?.Reference))
            query = query.Where(x => x.Reference == model.Data.Reference);
        if (model.Data?.DateRange != null)
            query = query.Where(x =>
                x.Created > model.Data.DateRange.DateFrom && x.Created < model.Data.DateRange.DateTo);
        if (!string.IsNullOrEmpty(model.Data?.Keyword))
        {
            string[] keywords = model.Data.Keyword.Split(' ');
            foreach (var keyword in keywords)
                query = query.Where(
                    x =>
                        x.Reference.Contains(keyword) ||
                        x.ChildFirstName.Contains(keyword) ||
                        x.ChildLastName.Contains(keyword) ||
                        x.ParentFirstName.Contains(keyword) ||
                        x.ParentLastName.Contains(keyword) ||
                        x.ParentNationalInsuranceNumber.Contains(keyword) ||
                        x.ParentNationalAsylumSeekerServiceNumber.Contains(keyword) ||
                        x.ParentEmail.Contains(keyword)
                );
        }

        return query.OrderBy(x => x.Created);

        // In case we need to order by status first and then by created date
        /* if (model.Data?.Statuses != null && model.Data.Statuses.Any())
        {
            return query.OrderBy(x => x.Status).ThenBy(x => x.Created);
        }
        else
        {
            return query.OrderBy(x => x.Created);
        } */
    }

    #region Private

    private string GetReference()
    {
        var unique = false;
        var nextReference = string.Empty;
        while (!unique)
        {
            nextReference = randomNumber.Next(1, referenceMaxValue).ToString();
            unique = _db.Applications.FirstOrDefault(x => x.Reference == nextReference) == null;
        }

        return nextReference;
    }


    private EligibilityCheckHash? GetHash(CheckEligibilityType type, Application data)
    {
        var age = DateTime.UtcNow.AddDays(-_hashCheckDays);
        var hash = CheckEligibilityGateway.GetHash(new CheckProcessData
        {
            DateOfBirth = data.ParentDateOfBirth.ToString("yyyy-MM-dd"),
            NationalInsuranceNumber = data.ParentNationalAsylumSeekerServiceNumber,
            NationalAsylumSeekerServiceNumber = data.ParentNationalInsuranceNumber,
            LastName = data.ParentLastName.ToUpper(),
            Type = type
        });
        return _db.EligibilityCheckHashes.FirstOrDefault(x => x.Hash == hash && x.TimeStamp >= age);
    }

    private async Task AddStatusHistory(Application application, ApplicationStatus applicationStatus)
    {
        var status = new Domain.ApplicationStatus
        {
            ApplicationStatusID = Guid.NewGuid().ToString(),
            ApplicationID = application.ApplicationID,
            Type = applicationStatus,
            TimeStamp = DateTime.UtcNow
        };
        await _db.ApplicationStatuses.AddAsync(status);
    }

    #endregion
}