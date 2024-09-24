using Microsoft.ApplicationInsights.DataContracts;
using System.Text;

namespace CheckYourEligibility.WebApp.Middleware
{
    public class RequestBodyLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestBodyLoggingMiddleware> _logger;

        public RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering(); // Allow multiple reads

            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0 &&
                context.Request.ContentType != null &&
                context.Request.ContentType.Contains("application/json"))
            {
                using (var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0; // Reset stream position
                }

                // Log the request body
                _logger.LogInformation($"Request Body: {requestBody}");

                // Optionally, attach to telemetry
                var telemetry = context.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Properties["RequestBody"] = requestBody;
                }
            }

            await _next(context);
        }
    }
}
