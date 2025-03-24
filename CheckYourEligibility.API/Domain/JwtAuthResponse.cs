namespace CheckYourEligibility.API.Domain
{
    public class JwtAuthResponse
    {
        public string access_token { get; set; }
        public string Token { get; set; }
        public Int32 expires_in { get; set; }
        public string token_type { get; set; }
    }
}