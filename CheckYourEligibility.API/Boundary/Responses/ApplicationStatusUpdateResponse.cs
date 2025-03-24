// Ignore Spelling: Fsm

using CheckYourEligibility.API.Domain.Enums;
using CheckYourEligibility.API.Boundary.Requests;

namespace CheckYourEligibility.API.Boundary.Responses
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
