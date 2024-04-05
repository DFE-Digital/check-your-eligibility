namespace CheckYourEligibility.Domain.Responses
{
    public class ApplicationSave
    {
        public string Id { get; set; }
        public string Reference { get; set; }
        public int LocalAuthority { get; set; }
        public int School { get; set; }
        public string ParentFirstName { get; set; }
        public string ParentLastName { get; set; }
        public string? ParentNationalInsuranceNumber { get; set; }
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }
        public string ParentDateOfBirth { get; set; }
        public string ChildFirstName { get; set; }
        public string ChildLastName { get; set; }
        public string ChildDateOfBirth { get; set; }
    }
}
