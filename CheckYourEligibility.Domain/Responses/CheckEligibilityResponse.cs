using System.Net.NetworkInformation;

namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityResponse
    {
        public StatusValue Data { get; set; }
        public CheckEligibilityResponseLinks Links { get; set; }
    }

    public class CheckEligibilityResponseBulk
    {
        public StatusValue Data { get; set; }
        public CheckEligibilityResponseBulkLinks Links { get; set; }
    }
}