using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using System.Dynamic;

namespace CheckYourEligibility.WebApp.Support
{
    public static class ResponseFormatter
    {

        public static dynamic GetResponseBadRequest(string message)
        {
            dynamic Data = new ExpandoObject();
            Data.Reason = message;
            dynamic response = new ExpandoObject();
            response.Data = Data;
            return response;
        }

        public static dynamic GetResponseStatus(CheckEligibilityStatus? status, string linkId = null)
        {
            dynamic Data = new ExpandoObject();
            Data.Status = $"{status}";
            dynamic Links = new ExpandoObject();
            if (!string.IsNullOrEmpty(linkId))
            {
                Links.Get_EligibilityCheck = $"{FSM.GetLink}{linkId}";
                Links.Put_EligibilityCheckProcess = $"{FSM.ProcessLink}{linkId}";
            }

            dynamic response = new ExpandoObject();
            response.Data = Data;
            if (!string.IsNullOrEmpty(linkId))
            {
                response.Links = Links;
            }
            return response;
        }

        public static dynamic GetResponseItem(CheckEligibilityItemFsm? item, string linkId = null)
        {
            dynamic Data = new ExpandoObject();
            Data.CheckEligibility = item;
            dynamic Links = new ExpandoObject();
            if (!string.IsNullOrEmpty(linkId))
            {
                Links.Get_EligibilityCheck = $"{FSM.GetLink}{linkId}";
                Links.Put_EligibilityCheckProcess = $"{FSM.ProcessLink}{linkId}";
            }

            dynamic response = new ExpandoObject();
            response.Data = Data;
            if (!string.IsNullOrEmpty(linkId))
            {
                response.Links = Links;
            }
            return response;
        }

        public static dynamic GetResponseMessage(string message)
        {
            dynamic Data = new ExpandoObject();
            Data.Message = $"{message}";
            dynamic Links = new ExpandoObject();
            

            dynamic response = new ExpandoObject();
            response.Data = Data;
            
            return response;
        }
    }
}
