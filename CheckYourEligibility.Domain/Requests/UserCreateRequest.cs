// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Requests
{
    public class UserCreateRequest
    {
       public UserData? Data { get; set; }
    }
    public class UserData
    {
        public string Email { get; set; }
        public string Reference { get; set; }
    }
}
