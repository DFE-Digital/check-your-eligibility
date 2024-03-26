using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Responses
{
    public class ApplicationFsm
    {
        public string Id { get; set; }
        public string Reference { get; set; }
        public ApplicationSchool School { get; set; }
        public string ParentFirstName { get; set; }
        public string ParentLastName { get; set; }
        public string? ParentNationalInsuranceNumber { get; set; }
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }
        public string ParentDateOfBirth { get; set; }
        public string ChildFirstName { get; set; }
        public string ChildLastName { get; set; }
        public string ChildDateOfBirth { get; set; }

        public class ApplicationSchool
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public SchoolLocalAuthority LocalAuthority { get; set; }

            public class SchoolLocalAuthority
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
        }
    }
}
