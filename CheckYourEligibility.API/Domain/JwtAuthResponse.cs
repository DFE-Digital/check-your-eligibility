namespace CheckYourEligibility.API.Domain;

public class JwtAuthResponse
{
    public string access_token { get; set; }
    public string Token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }
}