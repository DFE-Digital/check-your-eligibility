using Microsoft.AspNetCore.Http;
using Microsoft.ApplicationInsights;
using System;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TelemetryClient _telemetryClient;

        public ExceptionLoggingMiddleware(RequestDelegate next, TelemetryClient telemetryClient)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var exceptionTelemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(ex);

                // Add custom properties
                if (context.User.Identity.IsAuthenticated)
                {
                    exceptionTelemetry.Properties.Add("UserName", context.User.Identity.Name);
                }
                exceptionTelemetry.Properties.Add("RequestPath", context.Request.Path);

                _telemetryClient.TrackException(exceptionTelemetry);

                // Re-throw the exception after logging it
                throw;
            }
        }
    }
}
