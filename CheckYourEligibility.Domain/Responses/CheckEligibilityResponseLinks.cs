namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityResponseLinks
    {
        public string Get_EligibilityCheck { get; set; }
        public string Put_EligibilityCheckProcess { get; set; }
        public string Get_EligibilityCheckStatus { get; set; }
    }

    public class CheckEligibilityResponseBulkLinks
    {
        public string Get_Progress_Check { get; set; }
    }
}