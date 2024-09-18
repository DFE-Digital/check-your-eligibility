using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;

namespace CheckYourEligibility.WebApp.Telemetry
{
    public class UserTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Initialize(ITelemetry telemetry)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                telemetry.Context.User.AuthenticatedUserId = httpContext.User.Identity.Name;
            }
        }
    }
}
