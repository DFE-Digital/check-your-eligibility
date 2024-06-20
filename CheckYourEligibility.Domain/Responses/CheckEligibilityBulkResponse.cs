namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityBulkResponse
    {
        public IEnumerable<CheckEligibilityItemFsm> Data { get; set; }
    }
}
