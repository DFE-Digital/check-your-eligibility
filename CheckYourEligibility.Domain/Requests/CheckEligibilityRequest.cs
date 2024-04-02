namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityRequest
    {
       public CheckEligibilityRequestDataFsm? Data { get; set; }
    }
    public class CheckEligibilityRequestDataFsm
    {
        public string? NationalInsuranceNumber { get; set; }

        public string LastName { get; set; }

        public string DateOfBirth { get; set; }

        public string? NationalAsylumSeekerServiceNumber { get; set; }
    }
}
