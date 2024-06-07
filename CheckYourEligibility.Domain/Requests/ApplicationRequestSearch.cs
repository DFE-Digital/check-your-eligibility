// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CheckYourEligibility.Domain.Requests
{
    public class ApplicationRequestSearch
    {
        public ApplicationRequestSearchData? Data { get; set; }
    }

    public class ApplicationRequestSearchData
    {
        public int? localAuthority { get; set; }
        public int? School { get; set; }
        public ApplicationStatus? Status { get; set; }
        public string? ParentLastName { get; set; }
        public string? ParentNationalInsuranceNumber { get; set; }
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }
        public string? ParentDateOfBirth { get; set; }
        public string? ChildLastName { get; set; }
        public string? ChildDateOfBirth { get; set; }
    }
}
