namespace CheckYourEligibility.Domain
{
    public class JwtAuthResponse
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
    }
}