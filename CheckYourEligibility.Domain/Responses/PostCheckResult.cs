using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Responses
{
    public class PostCheckResult
    {
        public string Id { get; set; }
        public  CheckEligibilityStatus  Status { get; set; }
    }
}
