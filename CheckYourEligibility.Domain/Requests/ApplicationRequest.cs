﻿// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class ApplicationRequest
    {
       public ApplicationRequestData? Data { get; set; }
    }
    public class ApplicationRequestData
    {
        public CheckEligibilityType Type { get; set; }
        public int School { get; set; }
        public string ParentFirstName { get; set; }
        public string ParentLastName { get; set; }
        public string ParentEmail { get; set; }
        public string? ParentNationalInsuranceNumber { get; set; }
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }
        public string ParentDateOfBirth { get; set; }
        public string ChildFirstName { get; set; }
        public string ChildLastName { get; set; }
        public string ChildDateOfBirth { get; set; }
        public string? UserId { get; set; }
    }
}
