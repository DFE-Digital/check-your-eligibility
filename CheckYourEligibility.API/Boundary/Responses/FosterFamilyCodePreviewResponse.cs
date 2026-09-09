namespace CheckYourEligibility.API.Boundary.Responses
{

    public class FosterFamilyCodePreviewResponse
    {
        public DateTime ValidityStartDate { get; init; }

        public DateTime EligibilityConfirmed { get; init; }

        public string ReconfirmBetween { get; init; }

        public DateTime GracePeriodEndDate { get; init; }
    }
}