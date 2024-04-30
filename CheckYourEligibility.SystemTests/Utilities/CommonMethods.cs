
using CheckYourEligibility.Domain.Requests;


namespace CheckYourEligibility.SystemTests.Utilities
{
    public static class CommonMethods
    {

        public static string ExtractStringAfterLastSlash(string url)
        {
            // Find the last occurrence of '/'
            int lastSlashIndex = url.LastIndexOf('/');

            // Check if '/' is found in the URL
            if (lastSlashIndex >= 0 && lastSlashIndex < url.Length - 1)
            {
                // Extract the string after the last '/'
                return url.Substring(lastSlashIndex + 1);
            }

            // Return an empty string or handle the case where no '/' is found
            return string.Empty;
        }


        public static ApplicationRequest CreateAndProcessApplicationRequest()
        {
            // Create an instance of ApplicationRequestData
            ApplicationRequestData requestData = new ApplicationRequestData()
            {
                School = 107126,
                ParentFirstName = "John",
                ParentLastName = "Smith",
                ParentDateOfBirth = "01/01/1980", // Example Date of Birth in string format (YYYY-MM-DD)
                ChildFirstName = "Jane",
                ChildLastName = "Smith",
                ChildDateOfBirth = "01/01/2010",
                ParentNationalInsuranceNumber = "AB123456C",
            };

            // Create an instance of ApplicationRequest and assign the requestData to its Data property
            ApplicationRequest applicationRequest = new ApplicationRequest()
            {
                Data = requestData
            };

            // Return the populated ApplicationRequest object
            return applicationRequest;
        }

    }

}
