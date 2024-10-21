// Ignore Spelling: FSM

namespace CheckYourEligibility.Domain.Constants
{
    public static class CheckLinks
    {
        public const string GetLink = "/EligibilityCheck/";
        public const string ProcessLink = "/EligibilityCheck/processEligibilityCheck/";
        public const string Status = "status : ";

        public const string BulkCheckLink = "/EligibilityCheck/Bulk/";
        public const string BulkCheckProgress = "/CheckProgress";
        public const string BulkCheckResults = "/Results";
    }

    public static class ApplicationLinks
    {
        public const string GetLinkApplication = "/Application/";
    }

    public static class ApiUserLinks
    {
        public const string Link = "/ApiUsers/";
    }
}
