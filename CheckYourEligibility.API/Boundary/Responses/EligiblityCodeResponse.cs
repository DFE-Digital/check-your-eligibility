public class EligibilityCodeResponse
{
    public string EligibilityCode { get; init; }

    public string Status { get; init; }
    
    public DateTime EligibilityConfirmed { get; init; }

    public DateTime ReconfirmBetweenStart { get; init; }

    public DateTime ReconfirmBetweenEnd { get; init; }

    public DateTime GracePeriodEndDate { get; init; }
}