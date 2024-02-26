namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityItemFsmResponse
    {
        public CheckEligibilityItemFsm Data { get; set; }
    }
    public class CheckEligibilityItemFsm
    {
        public string NationalInsuranceNumber { get; set; }

        public string LastName { get; set; }

        public string DateOfBirth { get; set; }

        public string NationalAsylumSeekerServiceNumber { get; set; }

        public string Status { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
