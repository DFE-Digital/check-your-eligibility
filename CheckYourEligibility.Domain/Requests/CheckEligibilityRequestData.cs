namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityRequestData
    {
        public string NationalInsuranceNumber { get; set; } 

        public string LastName { get; set; }
               
        public string DateOfBirth { get; set; }
               
        public string NationalAsylumSeekerServiceNumber { get; set; } 
    }
}