namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityBulkResponse
    {
        public IEnumerable<CheckEligibilityItem> Data { get; set; }
    }
}
