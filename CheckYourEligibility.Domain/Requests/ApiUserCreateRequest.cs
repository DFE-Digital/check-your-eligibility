// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Requests
{
    public class ApiUserCreateRequest
    {
       public ApiUserData? Data { get; set; }
    }
    public class ApiUserData
    {
        public string LoginName { get; set; }
        public string Password { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
