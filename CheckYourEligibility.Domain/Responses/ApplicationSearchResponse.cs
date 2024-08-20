namespace CheckYourEligibility.Domain.Responses
{
    public class ApplicationSearchResponse
    {
        public IEnumerable<ApplicationResponse> Data { get; set; }

        public int TotalPages { get; set; }
    }
}
