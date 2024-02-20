namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityStatusResponse
    {
        public Data Data { get; set; }
    }
    public class Data
    {
        public string Status { get; set; }
    }
}
