namespace CheckYourEligibility.Domain.Responses
{
    public class CheckEligibilityBulkStatusResponse
    {
        public BulkStatus Data { get; set; }
    }

    public class BulkStatus
    {
        public int Total { get; set; }
        public int Complete { get; set; }
    }
}