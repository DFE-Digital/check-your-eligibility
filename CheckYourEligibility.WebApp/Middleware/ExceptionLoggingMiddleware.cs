namespace CheckYourEligibility.WebApp.Middleware
{
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
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
                _next = next;
                _telemetryClient = telemetryClient;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    var properties = new Dictionary<string, string> { { "EceApi", "ExceptionRaised" } };
                    var measure = new Dictionary<string, double> { { "EceException", 1.0 } };
                    // Log exception using TelemetryClient
                    _telemetryClient.TrackException(ex, properties, measure);

                    // Optionally, log additional information or rethrow
                    throw;
                }
            }
        }
    }
}
