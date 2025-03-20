using System.Net;
using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.WebApp.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckYourEligibility.WebApp.Controllers
{
    // [ExcludeFromCodeCoverage]
    [ApiController]
    public class Oauth2Controller : Controller
    {
        private IConfiguration _config;
        private readonly ILogger<Oauth2Controller> _logger;
        private readonly IAuthenticateUserUseCase _authenticateUserUseCase;

        public Oauth2Controller(IConfiguration config, ILogger<Oauth2Controller> logger, IAuthenticateUserUseCase authenticateUserUseCase)
        {
            _config = config;
            _logger = Guard.Against.Null(logger);
            _authenticateUserUseCase = Guard.Against.Null(authenticateUserUseCase);
        }

        [AllowAnonymous]
        [ProducesResponseType(typeof(JwtAuthResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
        [HttpPost("/oauth2/token")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> LoginForm([FromForm] SystemUser credentials)
        {

            if (!credentials.IsValidGrantType())
            {
                _logger.LogWarning(credentials.GetInvalidGrantTypeMessage().Replace(Environment.NewLine, ""));
            }
            
            var key = _config["Jwt:Key"];
            if (key == null)
            {
                _logger.LogError("Jwt:Key is required");
                return Unauthorized(new ErrorResponse { Errors = [new Error() {Title = ""}]});
            }

            var issuer = _config["Jwt:Issuer"];
            if (issuer == null)
            {
                _logger.LogError("Jwt:Issuer is required");
                return Unauthorized(new ErrorResponse { Errors = [new Error() {Title = ""}]});
            }

            // Initialize the credentials to set Identifier and Secret
            credentials.InitializeCredentials();

            if (!credentials.IsValid())
            {
                _logger.LogError("Either client_id/client_secret pair or Username/Password pair must be provided");
                return BadRequest(new ErrorResponse { Errors = [new Error() {Title = "Either client_id/client_secret pair or Username/Password pair must be provided"}]});
            }


            var secret = _config.GetSection($"Jwt:Clients:{credentials.Identifier}")["Secret"];
            if (secret == null)
            {
                // Try legacy user config path for backward compatibility
                secret = _config.GetSection($"Jwt:Users:{credentials.Identifier}").Get<string>();
                if (secret == null)
                {
                    _logger.LogError($"Authentication secret not found for identifier: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    return Unauthorized(new ErrorResponse { Errors = [new Error() {Title = ""}]});
                }
            }

            var jwtConfig = new JwtConfig
            {
                Key = key,
                Issuer = issuer,
                ExpectedSecret = secret
            };

            if (!string.IsNullOrEmpty(credentials.client_id) && !string.IsNullOrEmpty(credentials.scope))
            {
                jwtConfig.AllowedScopes = _config.GetSection($"Jwt:Clients:{credentials.Identifier}")["Scope"];
                if (string.IsNullOrEmpty(jwtConfig.AllowedScopes))
                {
                    _logger.LogError($"Allowed scopes not found for client: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    return Unauthorized(new ErrorResponse { Errors = [new Error() {Title = ""}]});
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
                return Unauthorized(new ErrorResponse { Errors = [new Error() {Title = ""}]});
            }
        }
    }
}