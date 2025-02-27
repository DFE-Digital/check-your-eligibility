using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.WebApp.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Login([FromBody] SystemUser login)
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

            var userPassword = _config.GetSection($"Jwt:Users:{login.Username}").Get<string>();
            if (userPassword == null)
            {
                _logger.LogError("UserPassword is required");
                return Unauthorized();
            }

            var jwtConfig = new JwtConfig
            {
                Key = key,
                Issuer = issuer,
                UserPassword = userPassword
            };

            var response = await _authenticateUserUseCase.Execute(login, jwtConfig);
            if (response != null)
            {
                _logger.LogInformation($"{login.Username.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} authenticated");
                return Ok(response);
            }
            else
            {
                _logger.LogError($"{login.Username.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "")} InvalidUser");
                return Unauthorized();
            }

        }
    }
}
