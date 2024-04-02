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

        public static dynamic GetResponseStatus(CheckEligibilityStatus? status, string? linkId = null)
        {
            dynamic data = new ExpandoObject();
            data.status = $"{status}";
            dynamic links = new ExpandoObject();
            if (!string.IsNullOrEmpty(linkId))
            {
                links.get_EligibilityCheck = $"{FSM.GetLink}{linkId}";
                links.put_EligibilityCheckProcess = $"{FSM.ProcessLink}{linkId}";
            }

            dynamic response = new ExpandoObject();
            response.data = data;
            if (!string.IsNullOrEmpty(linkId))
            {
                response.links = links;
            }
            return response;
        }

        public static dynamic GetResponseItem(CheckEligibilityItemFsm? item, string linkId = null)
        {
            dynamic data = new ExpandoObject();
            data.CheckEligibility = item;
            dynamic links = new ExpandoObject();
            if (!string.IsNullOrEmpty(linkId))
            {
                links.get_EligibilityCheck = $"{FSM.GetLink}{linkId}";
                links.put_EligibilityCheckProcess = $"{FSM.ProcessLink}{linkId}";
            }

            dynamic response = new ExpandoObject();
            response.data = data;
            if (!string.IsNullOrEmpty(linkId))
            {
                response.links = links;
            }
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

        public static dynamic GetResponseApplication(ApplicationSaveFsm? item)
        {
            dynamic data = new ExpandoObject();
            data.application = item;
            dynamic links = new ExpandoObject();
            links.get_Application = $"{FSM.GetLinkApplication}{item.Id}";

            dynamic response = new ExpandoObject();
            response.data = data;
            response.links = links;
            return response;
        }

        public static dynamic GetResponseApplication(ApplicationFsm? item)
        {
            dynamic data = new ExpandoObject();
            dynamic links = new ExpandoObject();

            data.application = item;
                links.get_Application = $"{FSM.GetLinkApplication}{item.Id}";

            dynamic response = new ExpandoObject();
            response.data = data;
            response.links = links;

            return response;
        }

        public static dynamic GetResponseApplication(IEnumerable<ApplicationFsm> item)
        {
            dynamic data = new ExpandoObject();

            data.application = item;
            dynamic response = new ExpandoObject();
            response.data = data;

            return response;
        }
    }
}
