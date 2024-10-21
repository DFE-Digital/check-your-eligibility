namespace CheckYourEligibility.Domain.Responses
{
    public class ApiUserSaveItemResponse
    {
        public string Data { get; set; }
        public ApiUserResponseLinks Links { get; set; }
    }

    public class ApiUserResponseLinks
    {
        public string get_ApiUser { get; set; }
        public string put_ApiUser { get; set; }
        public string delete_ApiUser { get; set; }
    }
}
