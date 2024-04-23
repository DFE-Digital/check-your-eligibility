// Ignore Spelling: Fsm

using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace CheckYourEligibility.Services
{
    public class BaseService
    {
        private readonly TelemetryClient _telemetry;
        public BaseService()
        {
            _telemetry = new TelemetryClient();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        protected void LogApiEvent<t1, t2>(string className, t1 data, t2 response, [CallerMemberName] string name = "")
        {

            var guid = Guid.NewGuid().ToString();
            string jsonString = JsonConvert.SerializeObject(data);
            string responseData = JsonConvert.SerializeObject(response);
            _telemetry.TrackEvent($"API {name} event",
                new Dictionary<string, string>
                {
                        {"LogId", guid},
                        {"Class", className},
                         {"Method", name},
                         {"Data", jsonString},
                    {"Response", responseData}
                });
        }

        protected async Task LogApiError(HttpResponseMessage task, string method, string uri, string data)
        {
            var guid = Guid.NewGuid().ToString();
            if (task.Content != null)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                _telemetry.TrackEvent($"API {method} failure",
                    new Dictionary<string, string>
                    {
                        {"LogId", guid},
                         {"Response Code", task.StatusCode.ToString()},
                        {"Address", uri},
                         {"Request Data", data},
                         {"Response Data", jsonString}
                    });
            }
            else
            {
                _telemetry.TrackEvent($"API Failure:-{method}",
                    new Dictionary<string, string> { { "LogId", guid }, { "Address", uri } });
            }
            throw new Exception($"API Failure:-{method} , your issue has been logged please use the following reference:- {guid}");
        }

        protected async Task LogApiError(HttpResponseMessage task, string method, string uri)
        {
            var guid = Guid.NewGuid().ToString();


            if (task.Content != null)
            {
                var jsonString = await task.Content.ReadAsStringAsync();
                _telemetry.TrackEvent($"API {method} failure",
                    new Dictionary<string, string>
                    {
                        {"LogId", guid},
                        {"Address", uri},
                        {"Response Code", task.StatusCode.ToString()},
                        {"Data", jsonString}
                    });
            }
            else
            {
                _telemetry.TrackEvent($"API {method} failure",
                    new Dictionary<string, string> { { "LogId", guid }, { "Address", uri } });
            }
        }
    }
}
