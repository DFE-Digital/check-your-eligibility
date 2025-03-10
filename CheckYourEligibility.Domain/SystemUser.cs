namespace CheckYourEligibility.Domain
{
    public class SystemUser
    {
        // Primary identifiers (OAuth2 standard names)
        public string? Identifier { get; set; }  // Can store either client_id or username
        public string? Secret { get; set; }      // Can store either client_secret or password
        public string? Scope { get; set; }

        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }

        // Legacy properties for backward compatibility
        public string? Username { get; set; }
        public string? Password { get; set; }

        public SystemUser()
        {
            // Default constructor for deserialization
        }

        /// <summary>
        /// Initialize with client credentials or username/password.
        /// Sets the Identifier and Secret properties based on available credentials.
        /// </summary>
        public void InitializeCredentials()
        {
            Identifier = !string.IsNullOrEmpty(ClientId) ? ClientId : Username;
            Secret = !string.IsNullOrEmpty(ClientSecret) ? ClientSecret : Password;
        }

        public bool IsValid()
        {
            return (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret)) ||
                (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));
        }
    }
}