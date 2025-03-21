namespace CheckYourEligibility.Domain
{
    public class SystemUser
    {
        // Primary identifiers (OAuth2 standard names)
        public string? scope { get; set; }
        public string? grant_type { get; set; }

        public string client_id { get; set; }
        public string client_secret { get; set; }

        // Legacy properties for backward compatibility

        public SystemUser()
        {
            // Default constructor for deserialization
        }
    }
}