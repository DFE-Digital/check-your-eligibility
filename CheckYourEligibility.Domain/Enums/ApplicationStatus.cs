// Ignore Spelling: Fsm

using System.ComponentModel;

namespace CheckYourEligibility.Domain.Enums
{
    public enum ApplicationStatus
    {
        [Description("Entitled")]
        Entitled,
        [Description("Receiving Entitlemen")]
        Receiving,
        [Description("Evidence Needed")]
        EvidenceNeeded,
        [Description("Sent for Review")]
        SentForReview,
        [Description("Reviewed Entitled")]
        ReviewedEntitled,
        [Description("Reviewed Not Entitled")]
        ReviewedNotEntitled,
    }
}
