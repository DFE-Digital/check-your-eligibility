using System;

namespace CheckYourEligibility.SystemTests.Utilities.Models
{
    public class EligibilityResponse
    {
        public string nationalInsuranceNumber { get; set; }
        public string lastName { get; set; }
        public string dateOfBirth { get; set; }
        public string nationalAsylumSeekerServiceNumber { get; set; }
        public string status { get; set; }
        public DateTime created { get; set; }
    }

    public class Links
    {
        public string get_EligibilityCheck { get; set; }
        public string put_EligibilityCheckProcess { get; set; }
        public object get_EligibilityCheckStatus { get; set; }
    }

    public class CheckEligibilityResponseModel
    {
        public EligibilityResponse data { get; set; }
        public Links links { get; set; }
    }
}
