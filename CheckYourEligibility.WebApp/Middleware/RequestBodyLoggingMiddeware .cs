using Microsoft.ApplicationInsights.DataContracts;


namespace CheckYourEligibility.WebApp.Middleware
{

    public class RequestBodyLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestBodyLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Keep the original response stream
            var originalBodyStream = context.Response.Body;

            // Create a new memory stream to hold the response
            using (var body = new MemoryStream())
            {
                context.Request.Body = body;

                // Invoke the next middleware in the pipeline
                await _next(context);

                // Read the response body
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(context.Request.Body).ReadToEndAsync();

                // Attach the response body to telemetry
                var telemetry = context.Features.Get<RequestTelemetry>();
                if (telemetry != null)
                {
                    telemetry.Properties["RequestBody"] = responseText;
                }

                // Reset the stream position and copy it back to the original stream
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                await body.CopyToAsync(originalBodyStream);
            }
        }
    }
}