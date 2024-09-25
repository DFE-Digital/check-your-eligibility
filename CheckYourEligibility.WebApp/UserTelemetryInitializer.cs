using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace CheckYourEligibility.WebApp.Telemetry
{
    public class UserTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserTelemetryInitializer> _logger;

        public UserTelemetryInitializer(IHttpContextAccessor httpContextAccessor, ILogger<UserTelemetryInitializer> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public void Initialize(ITelemetry telemetry)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                _logger.LogWarning("UserTelemetryInitializer: HttpContext is null.");
                return;
            }

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var user = httpContext.User;

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
                var userName = user.Identity.Name;

                // Log for debugging
                _logger.LogInformation("UserTelemetryInitializer: UserId={UserId}, Email={UserEmail}, Name={UserName}", userId, userEmail, userName);

                if (!string.IsNullOrEmpty(userId))
                {
                    telemetry.Context.User.AuthenticatedUserId = userId;
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    telemetry.Context.User.AccountId = userEmail;
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    telemetry.Context.User.Id = userName;
                }
            }
            else
            {
                _logger.LogWarning("UserTelemetryInitializer: User is not authenticated.");
            }
        }
    }
}
