// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Enums
{
    public enum ApplicationStatus
    {
        Open, //Entitled
        Receiving,//
        DocumentNeeded,//EvidenceNeeded
        PendingApproval,//SentForReview
        ReviewedEntitled,
        ReviewedNotEntitled,
    }
}
