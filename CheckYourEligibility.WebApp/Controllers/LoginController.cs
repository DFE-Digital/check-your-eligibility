using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.WebApp.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.WebApp.Controllers
{
    // [ExcludeFromCodeCoverage]
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private IConfiguration _config;
        private readonly ILogger<LoginController> _logger;
        private readonly IAuthenticateUserUseCase _authenticateUserUseCase;

        public LoginController(IConfiguration config, ILogger<LoginController> logger, IAuthenticateUserUseCase authenticateUserUseCase)
        {
            _config = config;
            _logger = Guard.Against.Null(logger);
            _authenticateUserUseCase = Guard.Against.Null(authenticateUserUseCase);
        }

        [AllowAnonymous]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> LoginJson([FromBody] SystemUser credentials)
        {
            if (!credentials.IsValidGrantType())
            {
                _logger.LogWarning(credentials.GetInvalidGrantTypeMessage().Replace(Environment.NewLine, ""));
            }
            return await AuthenticateUser(credentials);
        }

        [AllowAnonymous]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> LoginForm([FromForm] SystemUser credentials)
        {

            if (!credentials.IsValidGrantType())
            {
                _logger.LogWarning(credentials.GetInvalidGrantTypeMessage().Replace(Environment.NewLine, ""));
            }
            return await AuthenticateUser(credentials);
        }

        private async Task<IActionResult> AuthenticateUser(SystemUser credentials)
        {
            var key = _config["Jwt:Key"];
            if (key == null)
            {
                _logger.LogError("Jwt:Key is required");
                return Unauthorized();
            }

            var issuer = _config["Jwt:Issuer"];
            if (issuer == null)
            {
                _logger.LogError("Jwt:Issuer is required");
                return Unauthorized();
            }

            // Initialize the credentials to set Identifier and Secret
            credentials.InitializeCredentials();

            if (!credentials.IsValid())
            {
                _logger.LogError("Either ClientId/ClientSecret pair or Username/Password pair must be provided");
                return BadRequest("Either ClientId/ClientSecret pair or Username/Password pair must be provided");
            }


            var secret = _config.GetSection($"Jwt:Clients:{credentials.Identifier}")["Secret"];
            if (secret == null)
            {
                // Try legacy user config path for backward compatibility
                secret = _config.GetSection($"Jwt:Users:{credentials.Identifier}").Get<string>();
                if (secret == null)
                {
                    _logger.LogError($"Authentication secret not found for identifier: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    return Unauthorized();
                }
            }

            var jwtConfig = new JwtConfig
            {
                Key = key,
                Issuer = issuer,
                ExpectedSecret = secret
            };

            if (!string.IsNullOrEmpty(credentials.ClientId) && !string.IsNullOrEmpty(credentials.Scope))
            {
                jwtConfig.AllowedScopes = _config.GetSection($"Jwt:Clients:{credentials.Identifier}")["Scope"];
                if (string.IsNullOrEmpty(jwtConfig.AllowedScopes))
                {
                    _logger.LogError($"Allowed scopes not found for client: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    return Unauthorized();
                }
            }

            var response = await _authenticateUserUseCase.Execute(credentials, jwtConfig);
            if (response != null)
            {
                 _logger.LogInformation($"{credentials.Identifier.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} authenticated");
                return Ok(response);
            }
            else
            {
                _logger.LogWarning($"{credentials.Identifier.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} authentication failed");
                return Unauthorized();
            }
        }
    }
}