// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class ApplicationRequestSearch
    {
        public ApplicationRequestSearchData? Data { get; set; }
    }

    public class ApplicationRequestSearchData
    {
        public int? localAuthority { get; set; }
        public int? School { get; set; }
        public ApplicationStatus? Status { get; set; }
    }
}
