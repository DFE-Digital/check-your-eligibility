namespace CheckYourEligibility.Domain
{
    public class JwtConfig
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string UserPassword { get; set; }
    }
}