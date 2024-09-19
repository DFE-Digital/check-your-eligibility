﻿using Microsoft.ApplicationInsights.DataContracts;

namespace CheckYourEligibility.WebApp.Middleware
{
    public class ResponseBodyLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseBodyLoggingMiddleware> _logger;

        public ResponseBodyLoggingMiddleware(RequestDelegate next, ILogger<ResponseBodyLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using (var memStream = new MemoryStream())
            {
                context.Response.Body = memStream;

                await _next(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                // Log the response body
                _logger.LogInformation($"Response Body: {responseBody}");

                // Optionally, attach to telemetry
                var telemetry = context.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Properties["ResponseBody"] = responseBody;
                }

                await memStream.CopyToAsync(originalBodyStream);
            }
        }
    }
}
