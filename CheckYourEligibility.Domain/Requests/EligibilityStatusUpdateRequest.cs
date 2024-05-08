// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class EligibilityStatusUpdateRequest
    {
        public EligibilityCheckStatusData? Data { get; set; }
    }
    public class EligibilityCheckStatusData
    {
        public CheckEligibilityStatus Status { get; set; }
    }
}
