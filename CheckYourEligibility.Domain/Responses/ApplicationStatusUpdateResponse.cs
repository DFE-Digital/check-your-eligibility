// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Domain.Responses
{
    public class ApplicationStatusUpdateResponse
    {
        public ApplicationStatusDataResponse Data { get; set; }
    }

    public class ApplicationStatusDataResponse
    {
        public string Status { get; set; }
    }
}
