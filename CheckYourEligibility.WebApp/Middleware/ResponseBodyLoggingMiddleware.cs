using Microsoft.ApplicationInsights.DataContracts;


namespace CheckYourEligibility.WebApp.Middleware
{
    public class ResponseBodyLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseBodyLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Keep the original response stream
            var originalBodyStream = context.Response.Body;

            // Create a new memory stream to hold the response
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // Invoke the next middleware in the pipeline
                await _next(context);

                // Read the response body
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();

                // Attach the response body to telemetry
                var telemetry = context.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Properties["ResponseBody"] = responseText;
                }

                // Reset the stream position and copy it back to the original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }
}