namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityItemResponse
    {
        public CheckEligibilityItemFsm Data { get; set; }
        public CheckEligibilityResponseLinks Links { get; set; }
    }

    public class CheckEligibilityItemFsm
    {
        public string NationalInsuranceNumber { get; set; }

        public string LastName { get; set; }

        public string DateOfBirth { get; set; }

        public string NationalAsylumSeekerServiceNumber { get; set; }

        public string Status { get; set; }

        public DateTime Created { get; set; }
    }
}
