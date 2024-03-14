using System;

namespace CheckYourEligibility.SystemTests.Utilities.Models
{
    public class CheckEligibilityModel
    {
        public string nationalInsuranceNumber { get; set; }
        public string lastName { get; set; }
        public string dateOfBirth { get; set; }
        public string nationalAsylumSeekerServiceNumber { get; set; }
        public string status { get; set; }
        public DateTime created { get; set; }
    }

    public class CheckEligibilityDataModel
    {
        public CheckEligibilityModel CheckEligibility { get; set; }
    }

    public class CheckEligibilityResponseModel
    {
        public CheckEligibilityDataModel Data { get; set; }
    }
}
