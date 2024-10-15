using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Services
{
    public class CheckProcessData
    {
        public string? NationalInsuranceNumber { get; set; }

        public string LastName { get; set; }

        public string DateOfBirth { get; set; }

        public string? NationalAsylumSeekerServiceNumber { get; set; }

        public CheckEligibilityType Type { get; set; }
    }




}
