// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class ApplicationStatusUpdateRequest
    {
        public ApplicationStatusData? Data { get; set; }
    }

    public class ApplicationStatusData
    {
        public ApplicationStatus   Status { get; set; }
    }
}
