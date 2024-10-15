﻿// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class ApplicationRequestSearch
    {
        public ApplicationRequestSearchData? Data { get; set; }

        // Pagination properties at the request level
        public int PageNumber { get; set; } = 1; // Default to page 1
        public int PageSize { get; set; } = 10; // Default to 10 items per page
    }

    public class ApplicationRequestSearchData
    {
        public CheckEligibilityType Type { get; set; } = CheckEligibilityType.FreeSchoolMeals;
        public int? LocalAuthority { get; set; }
        public int? School { get; set; }
        public IEnumerable<ApplicationStatus>? Statuses { get; set; }
        public string? ParentLastName { get; set; }
        public string? ParentNationalInsuranceNumber { get; set; }
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }
        public string? ParentDateOfBirth { get; set; }
        public string? ChildLastName { get; set; }
        public string? ChildDateOfBirth { get; set; }
        public string? Reference { get; set; }
    }
}
