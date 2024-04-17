// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Enums
{
    public enum CheckEligibilityStatus
    {
        queuedForProcessing,
        parentNotFound,
        eligible,
        notEligible,
        DwpError
    }
}
