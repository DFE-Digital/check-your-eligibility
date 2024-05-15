// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;

namespace CheckYourEligibility.Domain.Requests
{
    public class AuditData
    {
        public AuditType Type { get; set; }
        public string typeId { get; set; }
        public string url { get; set; }
        public string method { get; set; }
        public string source { get; set; }
        public string authentication { get; set; }
    }
}
