using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Responses;
using System.Dynamic;

namespace CheckYourEligibility.WebApp.Support
{
    public static class ResponseFormatter
    {

        public static dynamic GetResponseBadRequest(string message)
        {
            dynamic data = new ExpandoObject();
            data.Reason = message;
            dynamic response = new ExpandoObject();
            response.data = data;
            return response;
        }

        public static dynamic GetResponseMessage(string message)
        {
            dynamic data = new ExpandoObject();
            data.Message = $"{message}";

            dynamic response = new ExpandoObject();
            response.data = data;
            
            return response;
        }

        public static object? GetSchoolsResponseMessage(IEnumerable<School>? results)
        {
            dynamic response = new ExpandoObject();
            response.data = results;

            return response;
        }
    }
}
