// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Requests
{
    public class SystemUserData
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public IList<string> Roles { get; set; }
    }
}
