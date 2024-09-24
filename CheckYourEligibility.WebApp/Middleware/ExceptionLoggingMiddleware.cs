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
                // Log exception using TelemetryClient
                _telemetryClient.TrackException(ex);

                // Optionally, log additional information or rethrow
                throw;
            }
        }
    }
}
