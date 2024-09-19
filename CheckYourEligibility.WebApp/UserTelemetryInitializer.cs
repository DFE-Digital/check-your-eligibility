using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

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
                var user = httpContext.User;

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
                var userName = user.Identity.Name;

                // Log claims for debugging
                Console.WriteLine($"Telemetry Initializer - UserId: {userId}, Email: {userEmail}, Name: {userName}");

                if (!string.IsNullOrEmpty(userId))
                {
                    telemetry.Context.User.AuthenticatedUserId = userId;
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    telemetry.Context.User.AccountId = userEmail;
                }

                telemetry.Context.User.Id = userName;
            }
        }

    }
}
