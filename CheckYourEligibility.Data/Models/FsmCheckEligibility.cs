

// Ignore Spelling: Fsm

namespace CheckYourEligibility.Data.Models
{
    public class FsmCheckEligibility
    {
        public string FsmCheckEligibilityID { get; set; }
        public FsmCheckEligibilityStatus Status { get; set; }

        public string NINumber { get; set; }

        public string LastName { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string NASSNumber { get; set; }

        public DateTime TimeStamp { get; set; }

    }
}
